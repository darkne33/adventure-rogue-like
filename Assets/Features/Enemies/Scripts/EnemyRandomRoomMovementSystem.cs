using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Features.Enemies.Scripts
{
    public sealed class EnemyRandomRoomMovementSystem : EnemyMovementSystemBase
    {
        private const int DestinationAttempts = 24;
        private const float PositionSampleDistance = 2f;
        private const float GroundRayHeight = 50f;
        private const float MinimumProgressDistance = 0.05f;

        private readonly Room _room;
        private readonly LayerMask _groundLayers;
        private readonly List<Collider> _groundColliders = new();
        private readonly NavMeshPath _candidatePath = new();
        private readonly float _minimumAttackDistance;
        private readonly float _maximumAttackDistance;
        private readonly float _arrivalDistance;
        private readonly float _minimumTravelDistance;

        private Vector3 _destination;
        private Vector3 _progressPosition;
        private float _stuckDuration;
        private float _nextSelectionTime;
        private bool _hasDestination;
        private bool _isHoldingPosition;

        public override bool CanAttack => _isHoldingPosition && IsNavigationReady;

        private bool IsNavigationReady => NavMeshAgent != null &&
            NavMeshAgent.isActiveAndEnabled && NavMeshAgent.isOnNavMesh;

        public EnemyRandomRoomMovementSystem(EnemyFacade enemy, CharacterFacade character,
            EnemyConfiguration configuration, NavMeshAgent navMeshAgent,
            IEnemyAnimationSystem animationSystem, Room room, LayerMask groundLayers,
            float minimumAttackDistance)
            : base(enemy, character, configuration, navMeshAgent, animationSystem)
        {
            _room = room;
            _groundLayers = groundLayers;
            _minimumAttackDistance = Mathf.Max(0f, minimumAttackDistance);
            _maximumAttackDistance = Mathf.Max(_minimumAttackDistance, configuration.DamageRange);
            _arrivalDistance = Mathf.Max(0.1f, configuration.RandomRoomArrivalDistance);
            _minimumTravelDistance = Mathf.Max(_arrivalDistance * 2f,
                configuration.RandomRoomMinimumTravelDistance);

            if (navMeshAgent != null)
            {
                navMeshAgent.autoBraking = true;
                navMeshAgent.stoppingDistance = _arrivalDistance * 0.5f;
            }

            if (room == null)
                return;

            foreach (Collider collider in room.GetComponentsInChildren<Collider>(false))
            {
                if (collider.enabled && !collider.isTrigger &&
                    (groundLayers.value & (1 << collider.gameObject.layer)) != 0)
                    _groundColliders.Add(collider);
            }
        }

        public override void Tick()
        {
            if (Time.timeScale <= 0f || Enemy.IsDead || Character == null ||
                Character.HealthSystem.IsDead || !IsNavigationReady || !CanMove())
                return;

            if (_isHoldingPosition && IsInsideAttackRange(Enemy.transform.position))
            {
                HoldPosition(true);
                return;
            }

            _isHoldingPosition = false;
            if (_hasDestination)
            {
                if (FlatSqrDistance(Enemy.transform.position, _destination) <=
                    _arrivalDistance * _arrivalDistance)
                {
                    _hasDestination = false;
                    if (IsInsideAttackRange(Enemy.transform.position))
                    {
                        HoldPosition(true);
                        return;
                    }
                }
                else if (NavMeshAgent.pathPending)
                {
                    UpdateManualNavigationPosition();
                    return;
                }
                else if (NavMeshAgent.hasPath &&
                         NavMeshAgent.pathStatus == NavMeshPathStatus.PathComplete &&
                         HasMovementProgress())
                {
                    UpdateManualNavigationPosition();
                    return;
                }
                else
                {
                    RetrySelection();
                    return;
                }
            }

            if (Time.time < _nextSelectionTime)
            {
                HoldPosition(IsInsideAttackRange(Enemy.transform.position));
                return;
            }

            if (!TrySelectDestination(out _destination) ||
                !NavMeshAgent.SetDestination(_destination))
            {
                RetrySelection();
                return;
            }

            _hasDestination = true;
            _progressPosition = Enemy.transform.position;
            _stuckDuration = 0f;
            UpdateManualNavigationPosition();
        }

        public override void Reset()
        {
            _hasDestination = false;
            _isHoldingPosition = false;
            _nextSelectionTime = 0f;
            _stuckDuration = 0f;
        }

        private bool TrySelectDestination(out Vector3 destination)
        {
            destination = default;
            if (_room == null || _groundColliders.Count == 0)
                return false;

            for (int attempt = 0; attempt < DestinationAttempts; attempt++)
            {
                Collider ground = _groundColliders[Random.Range(0, _groundColliders.Count)];
                if (ground == null || !ground.enabled || !ground.gameObject.activeInHierarchy)
                    continue;

                Bounds bounds = ground.bounds;
                float padding = Mathf.Max(0.1f, NavMeshAgent.radius);
                if (bounds.size.x <= padding * 2f || bounds.size.z <= padding * 2f)
                    continue;

                Vector3 candidate = new(
                    Random.Range(bounds.min.x + padding, bounds.max.x - padding),
                    bounds.max.y,
                    Random.Range(bounds.min.z + padding, bounds.max.z - padding));
                var filter = new NavMeshQueryFilter
                {
                    agentTypeID = NavMeshAgent.agentTypeID,
                    areaMask = NavMeshAgent.areaMask
                };
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit,
                        PositionSampleDistance, filter) ||
                    !IsInsideRoom(hit.position) || !IsInsideAttackRange(hit.position))
                    continue;

                float minimumTravel = attempt < DestinationAttempts / 2
                    ? _minimumTravelDistance
                    : _arrivalDistance * 2f;
                if (FlatSqrDistance(Enemy.transform.position, hit.position) <
                    minimumTravel * minimumTravel ||
                    !NavMeshAgent.CalculatePath(hit.position, _candidatePath) ||
                    _candidatePath.status != NavMeshPathStatus.PathComplete)
                    continue;

                bool pathStaysInRoom = true;
                foreach (Vector3 corner in _candidatePath.corners)
                {
                    if (IsInsideRoom(corner))
                        continue;

                    pathStaysInRoom = false;
                    break;
                }

                if (!pathStaysInRoom)
                    continue;

                destination = hit.position;
                return true;
            }

            return false;
        }

        private bool IsInsideRoom(Vector3 position)
        {
            return Physics.Raycast(position + Vector3.up * GroundRayHeight, Vector3.down,
                       out RaycastHit hit, GroundRayHeight * 2f, _groundLayers,
                       QueryTriggerInteraction.Ignore) &&
                   (hit.collider.transform == _room.transform ||
                    hit.collider.transform.IsChildOf(_room.transform));
        }

        private bool IsInsideAttackRange(Vector3 position)
        {
            float distance = FlatSqrDistance(position, Character.transform.position);
            return distance >= _minimumAttackDistance * _minimumAttackDistance &&
                   distance <= _maximumAttackDistance * _maximumAttackDistance;
        }

        private bool HasMovementProgress()
        {
            if (FlatSqrDistance(Enemy.transform.position, _progressPosition) >=
                MinimumProgressDistance * MinimumProgressDistance)
            {
                _progressPosition = Enemy.transform.position;
                _stuckDuration = 0f;
            }
            else
            {
                _stuckDuration += Time.fixedDeltaTime * Enemy.RelicTimeScale;
            }

            return _stuckDuration < Mathf.Max(0.1f, Configuration.RandomRoomStuckTimeout);
        }

        private void RetrySelection()
        {
            _hasDestination = false;
            _nextSelectionTime = Time.time + Mathf.Max(0.1f,
                Configuration.RandomRoomSelectionRetryDelay);
            HoldPosition(IsInsideAttackRange(Enemy.transform.position));
        }

        private void HoldPosition(bool canAttack)
        {
            if (NavMeshAgent.hasPath)
                NavMeshAgent.ResetPath();

            NavMeshAgent.velocity = Vector3.zero;
            _isHoldingPosition = canAttack;
            AnimationSystem.IdleAnimation();
        }

        private static float FlatSqrDistance(Vector3 first, Vector3 second)
        {
            float x = first.x - second.x;
            float z = first.z - second.z;
            return x * x + z * z;
        }
    }
}
