using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace Features.Enemies.Scripts
{
    public sealed class EnemyRicochetAttackSystem : IEnemyDamageSystem, IEnemyMovementSystem
    {
        private const float BoundaryClearance = 0.01f;
        private const int MaximumBouncesPerTick = 8;

        private readonly EnemyFacade _enemy;
        private readonly CharacterFacade _character;
        private readonly EnemyConfiguration _configuration;
        private readonly NavMeshAgent _agent;
        private readonly RaycastHit[] _castHits = new RaycastHit[16];
        private readonly Collider[] _overlapHits = new Collider[16];

        private BoxCollider _collider;
        private Vector3 _direction;
        private int _heading;
        private int _nextTurnStep;
        private float _turnCountdown;
        private float _damageCooldown;

        public bool CanAttack => true;

        public EnemyRicochetAttackSystem(EnemyFacade enemy, CharacterFacade character,
            EnemyConfiguration configuration, NavMeshAgent agent)
        {
            _enemy = enemy;
            _character = character;
            _configuration = configuration;
            _agent = agent;
        }

        public void Initialize()
        {
            _collider = _enemy.GetComponent<BoxCollider>();
            if (_agent == null || _collider == null || _enemy.Rigidbody == null ||
                _enemy.Rigidbody.isKinematic == false || _enemy.MovementVisual == null)
                throw new InvalidOperationException(
                    $"{_enemy.name} requires a NavMeshAgent, kinematic Rigidbody, BoxCollider and movement visual.");

            // The agent owns translation; the block keeps its fixed collider orientation.
            _agent.updateRotation = false;
            if (_agent.isActiveAndEnabled && _agent.isOnNavMesh)
                _agent.ResetPath();

            SetHeading(UnityEngine.Random.Range(0, 4));
            _nextTurnStep = -1;
            ScheduleNextTurn();
            UpdateVisualRotation();
        }

        public void Tick()
        {
            if (_agent == null || _agent.isActiveAndEnabled == false || _agent.isOnNavMesh == false)
                return;

            if (_enemy.IsDead || _character == null || _character.HealthSystem.IsDead ||
                _enemy.IsStopped || _enemy.IsRelicStunned || Time.timeScale <= 0f)
            {
                StopMovement();
                return;
            }

            if (_enemy.IsAggro == false)
            {
                // This enemy starts on spawn instead of chasing a nearby target first.
                _enemy.ActivateAggro();
                return;
            }

            _damageCooldown = Mathf.Max(0f,
                _damageCooldown - Time.deltaTime * _enemy.RelicTimeScale);

            // InitializeNavigation keeps this disabled while the spawn-rise tween runs.
            // Enable it only after the spawn stop has been released.
            if (_agent.updatePosition == false)
            {
                _agent.nextPosition = _enemy.transform.position;
                _agent.updatePosition = true;
            }

            float movementDeltaTime = Time.unscaledDeltaTime;
            UpdateDirection(movementDeltaTime);
            CheckContact(_agent.nextPosition);

            float remainingDistance = Mathf.Max(0f, _configuration.Speed) * movementDeltaTime;
            for (int bounce = 0; bounce < MaximumBouncesPerTick && remainingDistance > 0.001f; bounce++)
            {
                Vector3 position = _agent.nextPosition;
                bool hitWall = TryFindBoundary(position, remainingDistance,
                    out float wallDistance);
                float travel = hitWall
                    ? Mathf.Min(remainingDistance, Mathf.Max(0f, wallDistance - BoundaryClearance))
                    : remainingDistance;
                CheckSweptContact(position, travel);
                // No destinations, acceleration ramps or physics position writes:
                // the agent constrains each constant-speed step to the room NavMesh.
                _agent.Move(_direction * travel);
                remainingDistance -= travel;

                if (hitWall == false)
                    break;

                // Spend the rest of this frame's distance after reflecting, without
                // waiting for another frame or slowing down at the boundary.
                SetHeading(_heading + 2);
                _enemy.PlayCombatSound(Features.Sounds.SoundId.HazardBounce);
            }

            UpdateVisualRotation();
            CheckContact(_agent.nextPosition);
        }

        public UniTask Execute(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested || _enemy.IsDead ||
                _character == null || _character.HealthSystem.IsDead || _enemy.IsStopped ||
                _enemy.IsAggro == false || _enemy.CanAttack == false || _damageCooldown > 0f)
                return UniTask.CompletedTask;

            _damageCooldown = Mathf.Max(0.05f, _configuration.DamageCooldown);
            _character.ReceiveDamage(_configuration.Damage, _enemy);
            return UniTask.CompletedTask;
        }

        // Movement and swept contact damage share Update; there is no separate attack loop.
        public UniTask Tick(CancellationToken cancellationToken) => UniTask.CompletedTask;

        public void Reset()
        {
            // Preserve the trajectory when spawning, pausing or recovering from a relic stun.
        }

        public void OnAttackFinished()
        {
        }

        private void ScheduleNextTurn()
        {
            Vector2 interval = _configuration.RicochetTurnInterval;
            float minimum = Mathf.Max(0.1f, Mathf.Min(interval.x, interval.y));
            float maximum = Mathf.Max(minimum, Mathf.Max(interval.x, interval.y));
            _turnCountdown = UnityEngine.Random.Range(minimum, maximum);
        }

        private void UpdateDirection(float deltaTime)
        {
            _turnCountdown -= deltaTime;
            if (_turnCountdown <= 0f)
            {
                SetHeading(_heading + _nextTurnStep);
                _nextTurnStep = -_nextTurnStep;
                ScheduleNextTurn();
            }
        }

        private void SetHeading(int heading)
        {
            // Integer quarter-turns keep movement exactly on the world X/Z axes,
            // including bounces from angled NavMesh boundaries.
            _heading = heading & 3;
            _direction = _heading switch
            {
                0 => Vector3.forward,
                1 => Vector3.right,
                2 => Vector3.back,
                _ => Vector3.left
            };
        }

        private void UpdateVisualRotation()
        {
            _enemy.MovementVisual.rotation = Quaternion.Euler(0f, _heading * 90f, 0f);
        }

        private bool TryFindBoundary(Vector3 position, float distance,
            out float wallDistance)
        {
            wallDistance = distance;
            if (_agent.Raycast(position + _direction * (distance + BoundaryClearance),
                    out NavMeshHit hit) == false)
                return false;

            Vector3 normal = hit.normal;
            normal.y = 0f;
            if (normal.sqrMagnitude < 0.001f || Vector3.Dot(_direction, normal) >= -0.001f)
                return false;

            Vector3 offset = hit.position - position;
            offset.y = 0f;
            wallDistance = offset.magnitude;
            return true;
        }

        private void CheckContact(Vector3 position)
        {
            if (_damageCooldown > 0f)
                return;

            int count = Physics.OverlapBoxNonAlloc(GetCenter(position), GetHalfExtents(),
                _overlapHits, _enemy.transform.rotation, Physics.AllLayers, QueryTriggerInteraction.Collide);
            Collider[] hits = count == _overlapHits.Length
                ? Physics.OverlapBox(GetCenter(position), GetHalfExtents(), _enemy.transform.rotation,
                    Physics.AllLayers, QueryTriggerInteraction.Collide)
                : _overlapHits;
            if (hits != _overlapHits)
                count = hits.Length;

            for (int i = 0; i < count; i++)
            {
                if (hits[i].GetComponentInParent<CharacterFacade>() != _character)
                    continue;
                Execute(CancellationToken.None).Forget();
                return;
            }
        }

        private void CheckSweptContact(Vector3 position, float distance)
        {
            if (_damageCooldown > 0f || distance <= 0f)
                return;

            int count = Physics.BoxCastNonAlloc(GetCenter(position), GetHalfExtents(),
                _direction, _castHits, _enemy.transform.rotation, distance,
                Physics.AllLayers, QueryTriggerInteraction.Collide);
            RaycastHit[] hits = count == _castHits.Length
                ? Physics.BoxCastAll(GetCenter(position), GetHalfExtents(), _direction,
                    _enemy.transform.rotation, distance, Physics.AllLayers, QueryTriggerInteraction.Collide)
                : _castHits;
            if (hits != _castHits)
                count = hits.Length;

            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider.GetComponentInParent<CharacterFacade>() != _character)
                    continue;
                Execute(CancellationToken.None).Forget();
                return;
            }
        }

        private Vector3 GetCenter(Vector3 position) =>
            position + _enemy.transform.rotation * Vector3.Scale(_collider.center, _enemy.transform.lossyScale);

        private Vector3 GetHalfExtents()
        {
            Vector3 scale = _enemy.transform.lossyScale;
            scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            return Vector3.Scale(_collider.size * 0.5f, scale);
        }

        private void StopMovement()
        {
            _agent.velocity = Vector3.zero;
        }
    }
}
