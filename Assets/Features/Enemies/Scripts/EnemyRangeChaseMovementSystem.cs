using UnityEngine;
using UnityEngine.AI;

namespace Features.Enemies.Scripts
{
    /// <summary>
    /// Keeps a ranged enemy inside its configured combat-distance band.
    /// </summary>
    public sealed class EnemyRangeChaseMovementSystem : EnemyMovementSystemBase
    {
        private const float MinimumDirectionSqrMagnitude = 0.001f;
        private const float DistanceHysteresis = 1f;
        private const float CombatDistanceTolerance = 0.35f;
        private const float RepositionRetreatMargin = 1f;
        private const float RepositionReachedDistance = 0.75f;
        private const float MinimumRetreatStepDistance = 2f;
        private const float MaximumRetreatStepDistance = 6f;
        private const float RetreatDestinationReachedDistance = 1f;
        private const float RetreatPositionSampleDistance = 2f;
        private const float MinimumRetreatDistanceGain = 0.5f;
        private const float MaximumFallbackRetreatDistanceLoss = 0.25f;
        private const float RetreatSelectionRetryDelay = 0.25f;
        private const float NavigationPositionSampleDistance = 4f;

        private static readonly float[] RetreatDirectionAngles =
        {
            0f,
            45f,
            -45f,
            90f,
            -90f,
            135f,
            -135f
        };

        private readonly float _minimumDistance;
        private readonly float _maximumDistance;
        private readonly float _retreatStopDistance;
        private readonly float _retreatStartDistance;
        private readonly float _approachStopDistance;
        private readonly float _repositionDistance;
        private readonly float _repositionAngle;
        private readonly NavMeshPath _retreatPath = new();

        private bool _isRetreating;
        private bool _isApproaching;
        private bool _isRepositioning;
        private bool _isHoldingPosition;
        private bool _automaticNavigationEnabled;
        private bool _hasRetreatPosition;
        private bool _isRetreatBlocked;
        private float _nextRetreatSelectionTime;
        private Vector3 _retreatPosition;
        private Vector3 _repositionPosition;

        public override bool CanAttack
        {
            get
            {
                if ((_isRetreating && _isRetreatBlocked == false) ||
                    _isApproaching || _isRepositioning ||
                    _isHoldingPosition == false || Character == null)
                    return false;

                Vector3 offset = Enemy.transform.position - Character.transform.position;
                offset.y = 0f;
                float distanceSqr = offset.sqrMagnitude;
                float maximumAttackDistance =
                    _maximumDistance + CombatDistanceTolerance;
                return (_isRetreatBlocked ||
                        distanceSqr >=
                        _retreatStartDistance * _retreatStartDistance) &&
                       distanceSqr <=
                       maximumAttackDistance * maximumAttackDistance;
            }
        }

        public EnemyRangeChaseMovementSystem(EnemyFacade enemy, CharacterFacade character,
            EnemyConfiguration configuration, NavMeshAgent navMeshAgent,
            IEnemyAnimationSystem animationSystem)
            : base(enemy, character, configuration, navMeshAgent, animationSystem)
        {
            float configuredMinimumDistance = Mathf.Max(0f, Mathf.Min(
                configuration.RangeChaseMinimumDistance,
                configuration.RangeChaseMaximumDistance));
            float configuredMaximumDistance = Mathf.Max(
                configuration.RangeChaseMinimumDistance,
                configuration.RangeChaseMaximumDistance);
            float attackDistance = Mathf.Max(0f, configuration.DamageRange);

            _maximumDistance = attackDistance > 0f
                ? Mathf.Min(configuredMaximumDistance, attackDistance)
                : configuredMaximumDistance;
            _minimumDistance = Mathf.Min(configuredMinimumDistance, _maximumDistance);
            _retreatStopDistance = Mathf.Min(
                _minimumDistance + DistanceHysteresis,
                _maximumDistance);
            _retreatStartDistance = Mathf.Max(
                0f, _minimumDistance - CombatDistanceTolerance);
            _approachStopDistance = Mathf.Max(
                _maximumDistance - DistanceHysteresis,
                _minimumDistance);
            _repositionDistance = _maximumDistance;
            _repositionAngle = Mathf.Clamp(configuration.RangeChaseRepositionAngle, 0f, 180f);

            navMeshAgent.autoBraking = true;
            navMeshAgent.stoppingDistance = 0f;
        }

        public override void Tick()
        {
            if (CanMove() == false)
                return;

            EnableAutomaticNavigation();

            Vector3 awayFromCharacter = Enemy.transform.position - Character.transform.position;
            awayFromCharacter.y = 0f;
            float distance = awayFromCharacter.magnitude;

            if (_isRetreating)
            {
                if (distance < _retreatStopDistance)
                {
                    MoveAwayFromCharacter(awayFromCharacter);
                    return;
                }

                _isRetreating = false;
                _hasRetreatPosition = false;
                _isRetreatBlocked = false;
                _nextRetreatSelectionTime = 0f;
            }

            if (_isRepositioning)
            {
                float repositionRetreatDistance = Mathf.Max(
                    0f, _retreatStartDistance - RepositionRetreatMargin);
                if (distance < repositionRetreatDistance)
                {
                    _isRepositioning = false;
                    _isRetreating = true;
                    _hasRetreatPosition = false;
                    _isRetreatBlocked = false;
                    _nextRetreatSelectionTime = 0f;
                    MoveAwayFromCharacter(awayFromCharacter);
                    return;
                }

                if (MoveToRepositionPoint())
                    return;

                _isRepositioning = false;
            }

            if (distance < _retreatStartDistance)
            {
                _isApproaching = false;
                _isRetreating = true;
                _hasRetreatPosition = false;
                _isRetreatBlocked = false;
                _nextRetreatSelectionTime = 0f;
                MoveAwayFromCharacter(awayFromCharacter);
                return;
            }

            if (_isApproaching)
            {
                if (distance > _approachStopDistance)
                {
                    MoveUsingNavigation(Character.transform.position);
                    return;
                }

                _isApproaching = false;
            }

            if (distance > _maximumDistance)
            {
                _isApproaching = true;
                MoveUsingNavigation(Character.transform.position);
                return;
            }

            HoldPosition();
        }

        public override void OnAttackFinished()
        {
            Vector3 awayFromCharacter = Enemy.transform.position - Character.transform.position;
            awayFromCharacter.y = 0f;

            if (awayFromCharacter.sqrMagnitude < MinimumDirectionSqrMagnitude)
            {
                awayFromCharacter = Enemy.transform.forward;
                awayFromCharacter.y = 0f;

                if (awayFromCharacter.sqrMagnitude < MinimumDirectionSqrMagnitude)
                    awayFromCharacter = Vector3.forward;
            }

            float directionSign = Random.value < 0.5f ? -1f : 1f;
            Vector3 repositionDirection = Quaternion.AngleAxis(
                _repositionAngle * directionSign, Vector3.up) *
                awayFromCharacter.normalized;
            Vector3 desiredRepositionPosition =
                Character.transform.position + repositionDirection * _repositionDistance;
            bool hasRepositionPosition = TryResolveReachablePosition(
                desiredRepositionPosition, out _repositionPosition);
            _isRetreating = false;
            _isApproaching = false;
            _isRepositioning = hasRepositionPosition;
            _isHoldingPosition = false;
            _hasRetreatPosition = false;
            _isRetreatBlocked = false;
            _nextRetreatSelectionTime = 0f;
        }

        public override void Reset()
        {
            base.Reset();

            if (NavMeshAgent.isOnNavMesh && NavMeshAgent.hasPath)
                NavMeshAgent.ResetPath();

            if (NavMeshAgent.isOnNavMesh)
                NavMeshAgent.velocity = Vector3.zero;

            _isRetreating = false;
            _isApproaching = false;
            _isRepositioning = false;
            _isHoldingPosition = false;
            _hasRetreatPosition = false;
            _isRetreatBlocked = false;
            _nextRetreatSelectionTime = 0f;
        }

        private void HoldPosition()
        {
            if (NavMeshAgent.hasPath)
                NavMeshAgent.ResetPath();

            NavMeshAgent.velocity = Vector3.zero;
            _isHoldingPosition = true;
            AnimationSystem.IdleAnimation();
        }

        private void MoveAwayFromCharacter(Vector3 awayFromCharacter)
        {
            _isHoldingPosition = false;

            if (awayFromCharacter.sqrMagnitude < MinimumDirectionSqrMagnitude)
            {
                awayFromCharacter = Enemy.transform.forward;
                awayFromCharacter.y = 0f;

                if (awayFromCharacter.sqrMagnitude < MinimumDirectionSqrMagnitude)
                    awayFromCharacter = Vector3.forward;
            }

            float currentDistance = awayFromCharacter.magnitude;
            Vector3 awayDirection = awayFromCharacter.normalized;

            if (ShouldSelectRetreatPosition(currentDistance))
            {
                if (Time.time < _nextRetreatSelectionTime)
                {
                    HoldPosition();
                    return;
                }

                if (TrySelectRetreatPosition(awayDirection, currentDistance) == false)
                {
                    _isRetreatBlocked = true;
                    _nextRetreatSelectionTime =
                        Time.time + RetreatSelectionRetryDelay;
                    HoldPosition();
                    return;
                }

                _nextRetreatSelectionTime = 0f;
                _isRetreatBlocked = false;
            }

            if (MoveUsingNavigation(_retreatPosition))
                return;

            _hasRetreatPosition = false;
            _isRetreatBlocked = true;
            _nextRetreatSelectionTime = Time.time + RetreatSelectionRetryDelay;
            HoldPosition();
        }

        private bool ShouldSelectRetreatPosition(float currentDistance)
        {
            if (_hasRetreatPosition == false)
                return true;

            Vector3 toRetreatPosition = _retreatPosition - Enemy.transform.position;
            toRetreatPosition.y = 0f;
            if (toRetreatPosition.sqrMagnitude <=
                RetreatDestinationReachedDistance * RetreatDestinationReachedDistance)
            {
                return true;
            }

            Vector3 retreatOffsetFromCharacter =
                _retreatPosition - Character.transform.position;
            retreatOffsetFromCharacter.y = 0f;
            return retreatOffsetFromCharacter.magnitude <
                   currentDistance + MinimumRetreatDistanceGain;
        }

        private bool TrySelectRetreatPosition(Vector3 awayDirection, float currentDistance)
        {
            float remainingRetreatDistance = _retreatStopDistance - currentDistance;
            float retreatStepDistance = Mathf.Clamp(
                remainingRetreatDistance,
                MinimumRetreatStepDistance,
                MaximumRetreatStepDistance);
            float minimumCandidateDistance =
                currentDistance + MinimumRetreatDistanceGain;
            float bestCandidateDistance = float.NegativeInfinity;
            Vector3 bestCandidate = default;
            bool foundCandidate = false;
            bool bestCandidateProvidesDistanceGain = false;

            for (int i = 0; i < RetreatDirectionAngles.Length; i++)
            {
                Vector3 direction = Quaternion.AngleAxis(
                    RetreatDirectionAngles[i], Vector3.up) * awayDirection;
                Vector3 desiredPosition =
                    Enemy.transform.position + direction * retreatStepDistance;

                if (NavMesh.SamplePosition(
                        desiredPosition,
                        out NavMeshHit hit,
                        RetreatPositionSampleDistance,
                        NavMeshAgent.areaMask) == false)
                {
                    continue;
                }

                Vector3 candidateOffset = hit.position - Character.transform.position;
                candidateOffset.y = 0f;
                float candidateDistance = candidateOffset.magnitude;
                bool providesDistanceGain =
                    candidateDistance >= minimumCandidateDistance;
                if (providesDistanceGain == false &&
                    candidateDistance <
                    currentDistance - MaximumFallbackRetreatDistanceLoss)
                {
                    continue;
                }

                if (NavMeshAgent.CalculatePath(hit.position, _retreatPath) == false ||
                    _retreatPath.status != NavMeshPathStatus.PathComplete)
                {
                    continue;
                }

                Vector3 movementOffset = hit.position - Enemy.transform.position;
                movementOffset.y = 0f;
                if (movementOffset.sqrMagnitude <=
                    RetreatDestinationReachedDistance * RetreatDestinationReachedDistance)
                {
                    continue;
                }

                if (foundCandidate)
                {
                    if (bestCandidateProvidesDistanceGain && providesDistanceGain == false)
                        continue;

                    if (providesDistanceGain == bestCandidateProvidesDistanceGain &&
                        candidateDistance <= bestCandidateDistance)
                    {
                        continue;
                    }
                }

                bestCandidateDistance = candidateDistance;
                bestCandidate = hit.position;
                foundCandidate = true;
                bestCandidateProvidesDistanceGain = providesDistanceGain;
            }

            if (foundCandidate == false)
                return false;

            _retreatPosition = bestCandidate;
            _hasRetreatPosition = true;
            return true;
        }

        private bool TryResolveReachablePosition(
            Vector3 desiredPosition, out Vector3 reachablePosition)
        {
            if (NavMesh.SamplePosition(
                    desiredPosition,
                    out NavMeshHit hit,
                    NavigationPositionSampleDistance,
                    NavMeshAgent.areaMask) &&
                NavMeshAgent.CalculatePath(hit.position, _retreatPath) &&
                _retreatPath.status == NavMeshPathStatus.PathComplete)
            {
                reachablePosition = hit.position;
                return true;
            }

            reachablePosition = default;
            return false;
        }

        private bool MoveToRepositionPoint()
        {
            Vector3 offset = _repositionPosition - Enemy.transform.position;
            offset.y = 0f;

            if (offset.sqrMagnitude <= RepositionReachedDistance * RepositionReachedDistance)
            {
                HoldPosition();
                return false;
            }

            if (MoveUsingNavigation(_repositionPosition))
                return true;

            HoldPosition();
            return false;
        }

        private void EnableAutomaticNavigation()
        {
            if (_automaticNavigationEnabled)
                return;

            NavMeshAgent.nextPosition = Enemy.transform.position;
            NavMeshAgent.updatePosition = true;
            NavMeshAgent.updateRotation = true;
            _automaticNavigationEnabled = true;
        }

        private bool MoveUsingNavigation(Vector3 desiredPosition)
        {
            _isHoldingPosition = false;

            if (SetNavigationDestination(desiredPosition) == false)
            {
                if (NavMeshAgent.hasPath)
                    NavMeshAgent.ResetPath();

                NavMeshAgent.velocity = Vector3.zero;
                return false;
            }

            return true;
        }

    }
}
