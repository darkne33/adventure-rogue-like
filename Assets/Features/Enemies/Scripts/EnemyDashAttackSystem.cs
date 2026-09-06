using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace Features.Enemies.Scripts
{
    // One FixedUpdate owner for pursuit, aiming, dashing and recovery.
    public sealed class EnemyDashAttackSystem : IEnemyDamageSystem, IEnemyMovementSystem
    {
        private enum State
        {
            Pursuit,
            Windup,
            ReadyToDash,
            Dashing,
            Recovery,
            Finished
        }

        private const float NavigationSampleDistance = 4f;

        private readonly CharacterFacade _characterFacade;
        private readonly EnemyConfiguration _enemyConfiguration;
        private readonly EnemyFacade _enemyFacade;
        private readonly EnemyDashView _dashView;
        private readonly IEnemiesProvider _enemiesProvider;
        private readonly float _attackPreparationDuration;
        private readonly Collider[] _hitBuffer = new Collider[16];

        private Rigidbody _rigidbody;
        private NavMeshAgent _navMeshAgent;
        private SphereCollider _hitCollider;
        private Collider[] _colliders;
        private EnemyDashSkirmisherMovement _skirmisherMovement;
        private bool[] _wasTrigger;
        private State _state;
        private float _cooldown;
        private float _attackRange;
        private float _stopDistance;
        private float _stateTime;
        private float _remainingDashDistance;
        private float _lastDashStep;
        private bool _canDamage;
        private bool _hasPhysicsSnapshot;
        private bool _useGravity;
        private Quaternion _attackRotation;
        private Vector3 _dashDirection;
        private Vector3 _dashOrigin;
        private Vector3 _previousDashPosition;

        public bool CanAttack => true;

        private bool CanContinueAttack =>
            _enemyFacade != null && _enemyFacade.isActiveAndEnabled &&
            _enemyFacade.IsDead == false && _enemyFacade.CanAttack &&
            _characterFacade != null;

        public EnemyDashAttackSystem(CharacterFacade characterFacade, EnemyConfiguration enemyConfiguration,
            EnemyFacade enemyFacade, EnemyDashView dashView, float attackPreparationDuration,
            IEnemiesProvider enemiesProvider)
        {
            _characterFacade = characterFacade;
            _enemyConfiguration = enemyConfiguration;
            _enemyFacade = enemyFacade;
            _dashView = dashView;
            _enemiesProvider = enemiesProvider;
            _attackPreparationDuration = Mathf.Max(0f, attackPreparationDuration);
        }

        public void Initialize()
        {
            _rigidbody = _enemyFacade.Rigidbody;
            _navMeshAgent = _enemyFacade.GetComponent<NavMeshAgent>();
            _hitCollider = _enemyFacade.GetComponent<SphereCollider>();
            if (_hitCollider == null)
                throw new InvalidOperationException($"{_enemyFacade.name} requires a SphereCollider for its dash.");

            _colliders = _enemyFacade.GetComponentsInChildren<Collider>();
            _wasTrigger = new bool[_colliders.Length];
            _attackRange = Mathf.Min(Mathf.Max(0f, _enemyConfiguration.DamageRange),
                Mathf.Max(0f, _enemyConfiguration.DashDistance));
            _stopDistance = Mathf.Clamp(_enemyConfiguration.DistanceToStop > 0f
                ? _enemyConfiguration.DistanceToStop
                : _attackRange * 0.8f, 0f, _attackRange);
            _cooldown = Mathf.Max(0f, _enemyConfiguration.DamageCooldown);

            // The agent supplies steering only. It never writes the visible body's pose.
            _navMeshAgent.updatePosition = false;
            _navMeshAgent.updateRotation = false;
            _navMeshAgent.autoBraking = true;
            _navMeshAgent.stoppingDistance = _stopDistance;
            if (_enemyConfiguration.EnemyMovementType == EnemyMovementType.Skirmisher)
            {
                _skirmisherMovement = new EnemyDashSkirmisherMovement(_enemyFacade,
                    _characterFacade, _enemiesProvider, _navMeshAgent, _attackRange);
                _navMeshAgent.stoppingDistance = 0.25f;
            }
            _rigidbody.constraints |= RigidbodyConstraints.FreezeRotation;
            _enemyFacade.EnemyCollisionDetector.OnCollisionEnterEvent += ApplyDamage;
        }

        public void Tick()
        {
            if (_rigidbody == null)
                return;

            if (_state == State.Pursuit)
            {
                TickPursuit();
                return;
            }

            if (CanContinueAttack == false)
            {
                _canDamage = false;
                StopHorizontalMovement();
                _state = State.Finished;
                return;
            }

            switch (_state)
            {
                case State.Windup:
                    TickWindup();
                    break;
                case State.Dashing:
                    TickDash();
                    break;
                case State.Recovery:
                    HoldAttackRotation();
                    _stateTime += Time.fixedDeltaTime;
                    if (_stateTime >= Mathf.Max(0f, _enemyConfiguration.MovementPauseAfterAttack))
                        _state = State.Finished;
                    break;
                default:
                    HoldAttackRotation();
                    break;
            }
        }

        public async UniTask Tick(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _cooldown -= Time.deltaTime * _enemyFacade.RelicTimeScale;
                if (_state == State.Pursuit && CanContinueAttack &&
                    _enemyFacade.IsAggro && _enemyFacade.IsStopped == false &&
                    _cooldown <= 0f && GetFlatOffsetToCharacter().sqrMagnitude <= _attackRange * _attackRange)
                {
                    await Execute(cancellationToken);
                    _cooldown = Mathf.Max(0f, _enemyConfiguration.DamageCooldown);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        public async UniTask Execute(CancellationToken cancellationToken)
        {
            if (_state != State.Pursuit || CanContinueAttack == false ||
                _enemyFacade.IsAggro == false || _enemyFacade.IsStopped)
                return;

            try
            {
                CapturePhysics();
                _state = State.Windup;
                _stateTime = 0f;
                _attackRotation = Quaternion.LookRotation(GetFlatDirection(_rigidbody.rotation * Vector3.forward));
                _dashDirection = _attackRotation * Vector3.forward;
                if (_enemyConfiguration.DashTrackingDuration <= 0f)
                {
                    _dashDirection = GetFlatDirection(GetFlatOffsetToCharacter(), _dashDirection);
                    _attackRotation = Quaternion.LookRotation(_dashDirection, Vector3.up);
                }

                _enemyFacade.SetStop(true);
                StopHorizontalMovement();
                _enemyFacade.AnimationSystem.IdleAnimation();
                _enemyFacade.EffectsSystem.BeginAttackTelegraph(_attackPreparationDuration);

                await UniTask.WaitUntil(() => _state != State.Windup || CanContinueAttack == false,
                    cancellationToken: cancellationToken);
                if (CanContinueAttack == false || _state != State.ReadyToDash)
                    return;

                await _enemyFacade.EffectsSystem.CompleteAttackTelegraph(cancellationToken);
                if (CanContinueAttack == false)
                    return;

                BeginDash();
                await UniTask.WaitUntil(() => _state == State.Finished || CanContinueAttack == false,
                    cancellationToken: cancellationToken);
            }
            finally
            {
                FinishAttack();
            }
        }

        public void Reset()
        {
            if (_state != State.Pursuit || _navMeshAgent == null || _navMeshAgent.isOnNavMesh == false)
                return;

            _skirmisherMovement?.Reset();
            _navMeshAgent.nextPosition = _rigidbody.position;
            if (_navMeshAgent.hasPath)
                _navMeshAgent.ResetPath();
        }

        public void OnAttackFinished()
        {
        }

        private void TickPursuit()
        {
            if (_enemyFacade.IsDead || _characterFacade == null)
                return;

            if (_enemyFacade.IsStopped || _navMeshAgent.isOnNavMesh == false)
            {
                StopHorizontalMovement();
                _enemyFacade.AnimationSystem.IdleAnimation();
                return;
            }

            Vector3 toCharacter = GetFlatOffsetToCharacter();
            if (_enemyFacade.IsAggro == false &&
                toCharacter.sqrMagnitude <= Mathf.Pow(Mathf.Max(0.1f, _enemyConfiguration.AggroRange), 2f))
            {
                _enemyFacade.ActivateAggro();
                StopHorizontalMovement();
                return;
            }

            _navMeshAgent.nextPosition = _rigidbody.position;
            bool isSkirmishing = _skirmisherMovement != null && _enemyFacade.IsAggro;
            if (isSkirmishing == false && _enemyFacade.IsAggro &&
                toCharacter.sqrMagnitude <= _stopDistance * _stopDistance)
            {
                if (_navMeshAgent.hasPath)
                    _navMeshAgent.ResetPath();
                StopHorizontalMovement();
                RotateBodyTowards(toCharacter, _enemyConfiguration.RotationSpeed);
                _enemyFacade.AnimationSystem.IdleAnimation();
                return;
            }

            Vector3 destination = _characterFacade.transform.position;
            if (isSkirmishing && _skirmisherMovement.TryGetDestination(
                    Time.fixedDeltaTime * _enemyFacade.RelicTimeScale, out destination) == false)
            {
                if (_navMeshAgent.hasPath)
                    _navMeshAgent.ResetPath();
                StopHorizontalMovement();
                RotateBodyTowards(toCharacter, _enemyConfiguration.RotationSpeed);
                _enemyFacade.AnimationSystem.IdleAnimation();
                return;
            }

            if (NavMesh.SamplePosition(destination, out NavMeshHit hit,
                    NavigationSampleDistance, _navMeshAgent.areaMask) == false ||
                _navMeshAgent.SetDestination(hit.position) == false)
            {
                StopHorizontalMovement();
                _enemyFacade.AnimationSystem.IdleAnimation();
                return;
            }

            Vector3 velocity = _navMeshAgent.desiredVelocity;
            velocity.y = 0f;
            velocity = Vector3.ClampMagnitude(velocity, _navMeshAgent.speed);
            _rigidbody.linearVelocity = new Vector3(velocity.x, _rigidbody.linearVelocity.y, velocity.z);
            RotateBodyTowards(velocity, _enemyConfiguration.RotationSpeed);
            if (velocity.sqrMagnitude > 0.001f)
                _enemyFacade.AnimationSystem.RunAnimation();
            else
                _enemyFacade.AnimationSystem.IdleAnimation();
        }

        private void TickWindup()
        {
            float trackingDuration = Mathf.Clamp(_enemyConfiguration.DashTrackingDuration,
                0f, _attackPreparationDuration);
            if (_stateTime < trackingDuration)
            {
                Vector3 targetDirection = GetFlatDirection(GetFlatOffsetToCharacter(), _dashDirection);
                _attackRotation = Quaternion.RotateTowards(_attackRotation,
                    Quaternion.LookRotation(targetDirection, Vector3.up),
                    Mathf.Max(0f, _enemyConfiguration.DashRotationSpeed) * Time.fixedDeltaTime);
                _dashDirection = _attackRotation * Vector3.forward;
            }

            // Once tracking ends, neither the heading nor the attack line follows the player.
            HoldAttackRotation();
            _stateTime += Time.fixedDeltaTime;
            float progress = _attackPreparationDuration > 0f
                ? Mathf.Clamp01(_stateTime / _attackPreparationDuration) : 1f;
            _dashView?.ShowTelegraph(_dashDirection,
                Mathf.Max(0f, _enemyConfiguration.DashDistance), progress);
            if (_stateTime >= _attackPreparationDuration)
                _state = State.ReadyToDash;
        }

        private void BeginDash()
        {
            _dashOrigin = _rigidbody.position;
            _previousDashPosition = _dashOrigin;
            _remainingDashDistance = Mathf.Max(0f, _enemyConfiguration.DashDistance);
            _lastDashStep = 0f;
            _rigidbody.useGravity = false;
            _rigidbody.linearVelocity = Vector3.zero;
            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] != null && _colliders[i].attachedRigidbody == _rigidbody)
                    _colliders[i].isTrigger = true;
            }

            _state = State.Dashing;
            _canDamage = true;
            _enemyFacade.AnimationSystem.AttackAnimation();
            _dashView?.StartDash();
        }

        private void TickDash()
        {
            Vector3 position = _rigidbody.position;
            CheckDashHit(_previousDashPosition, position);
            if (CanContinueAttack == false)
            {
                _canDamage = false;
                StopHorizontalMovement();
                _state = State.Finished;
                return;
            }

            _previousDashPosition = position;
            _remainingDashDistance = Mathf.Max(0f, _remainingDashDistance - _lastDashStep);

            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.MoveRotation(_attackRotation);
            if (_remainingDashDistance <= 0f)
            {
                _canDamage = false;
                _rigidbody.linearVelocity = Vector3.zero;
                RestorePhysics();
                _dashView?.StopDash();
                _enemyFacade.AnimationSystem.IdleAnimation();
                _stateTime = 0f;
                _state = State.Recovery;
                return;
            }

            _lastDashStep = Mathf.Min(_remainingDashDistance,
                Mathf.Max(0.01f, _enemyConfiguration.DashSpeed) * Time.fixedDeltaTime);
            _rigidbody.linearVelocity = _dashDirection * (_lastDashStep / Time.fixedDeltaTime);
        }

        private void CheckDashHit(Vector3 from, Vector3 to)
        {
            if (_canDamage == false)
                return;

            Vector3 scale = _hitCollider.transform.lossyScale;
            float radius = _hitCollider.radius * Mathf.Max(Mathf.Abs(scale.x),
                Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            Vector3 centerOffset = _attackRotation * Vector3.Scale(_hitCollider.center, scale);
            from += centerOffset;
            to += centerOffset;
            int count = Physics.OverlapCapsuleNonAlloc(from, to, radius, _hitBuffer,
                Physics.AllLayers, QueryTriggerInteraction.Collide);
            Collider[] hits = count == _hitBuffer.Length
                ? Physics.OverlapCapsule(from, to, radius, Physics.AllLayers, QueryTriggerInteraction.Collide)
                : _hitBuffer;
            if (hits != _hitBuffer)
                count = hits.Length;

            for (int i = 0; i < count; i++)
            {
                if (hits[i].GetComponentInParent<CharacterFacade>() != _characterFacade)
                    continue;
                ApplyDamage();
                return;
            }
        }

        private void ApplyDamage()
        {
            if (_state != State.Dashing || _canDamage == false || CanContinueAttack == false)
                return;

            _canDamage = false;
            if (_characterFacade.ReceiveDamage(_enemyConfiguration.Damage, _enemyFacade) == false)
                return;

            Vector3 pushDirection = Vector3.Cross(Vector3.up, _dashDirection).normalized;
            if (Vector3.Dot(_characterFacade.transform.position - _dashOrigin, pushDirection) < 0f)
                pushDirection = -pushDirection;

            Vector3 knockback = pushDirection * Mathf.Max(0f, _enemyConfiguration.DashKnockbackForce) +
                                Vector3.up * Mathf.Max(0f, _enemyConfiguration.DashKnockbackUpwardForce);
            _characterFacade.Rigidbody.AddForce(knockback, ForceMode.Impulse);
        }

        private void CapturePhysics()
        {
            _useGravity = _rigidbody.useGravity;
            for (int i = 0; i < _colliders.Length; i++)
                _wasTrigger[i] = _colliders[i] != null && _colliders[i].isTrigger;
            _hasPhysicsSnapshot = true;
        }

        private void RestorePhysics()
        {
            if (_hasPhysicsSnapshot == false)
                return;

            if (_rigidbody != null)
                _rigidbody.useGravity = _useGravity;
            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] != null && _colliders[i].attachedRigidbody == _rigidbody)
                    _colliders[i].isTrigger = _wasTrigger[i];
            }

            _hasPhysicsSnapshot = false;
        }

        private void FinishAttack()
        {
            _canDamage = false;
            RestorePhysics();
            if (_dashView != null)
                _dashView.StopDash();
            if (_enemyFacade == null)
                return;

            _enemyFacade.EffectsSystem.ClearAttackTelegraph();
            StopHorizontalMovement();
            _enemyFacade.AnimationSystem.IdleAnimation();
            if (_enemyFacade.IsDead == false)
            {
                // Synchronize once, after recovery, while pursuit is still suspended.
                _enemyFacade.SyncNavigationPosition();
                _rigidbody.rotation = _attackRotation;
            }

            _state = State.Pursuit;
            _enemyFacade.SetStop(false);
        }

        private void HoldAttackRotation()
        {
            StopHorizontalMovement();
            _rigidbody.MoveRotation(_attackRotation);
        }

        private void StopHorizontalMovement()
        {
            if (_rigidbody == null)
                return;
            Vector3 velocity = _rigidbody.linearVelocity;
            _rigidbody.linearVelocity = new Vector3(0f, velocity.y, 0f);
            _rigidbody.angularVelocity = Vector3.zero;
        }

        private void RotateBodyTowards(Vector3 direction, float rotationSpeed)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.001f)
                return;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.MoveRotation(Quaternion.RotateTowards(_rigidbody.rotation,
                Quaternion.LookRotation(direction.normalized, Vector3.up),
                Mathf.Max(0f, rotationSpeed) * Time.fixedDeltaTime));
        }

        private Vector3 GetFlatOffsetToCharacter()
        {
            Vector3 direction = _characterFacade.transform.position - _rigidbody.position;
            direction.y = 0f;
            return direction;
        }

        private static Vector3 GetFlatDirection(Vector3 direction, Vector3 fallback = default)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
                return direction.normalized;
            fallback.y = 0f;
            return fallback.sqrMagnitude > 0.001f ? fallback.normalized : Vector3.forward;
        }
    }
}
