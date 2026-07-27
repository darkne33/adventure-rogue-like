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

        private readonly float _minimumDistance;
        private readonly float _maximumDistance;
        private readonly float _retreatStopDistance;
        private readonly float _retreatStartDistance;
        private readonly float _approachStopDistance;
        private readonly float _repositionDistance;
        private readonly float _repositionAngle;

        private bool _isRetreating;
        private bool _isApproaching;
        private bool _isRepositioning;
        private bool _isHoldingPosition;
        private bool _automaticNavigationEnabled;
        private Vector3 _repositionPosition;

        public override bool CanAttack
        {
            get
            {
                if (_isRetreating || _isApproaching || _isRepositioning ||
                    _isHoldingPosition == false || Character == null)
                    return false;

                Vector3 offset = Enemy.transform.position - Character.transform.position;
                offset.y = 0f;
                float distanceSqr = offset.sqrMagnitude;
                float maximumAttackDistance =
                    _maximumDistance + CombatDistanceTolerance;
                return distanceSqr >=
                       _retreatStartDistance * _retreatStartDistance &&
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
            _retreatStopDistance = _maximumDistance;
            _retreatStartDistance = Mathf.Max(
                0f, _minimumDistance - CombatDistanceTolerance);
            _approachStopDistance = Mathf.Max(
                _maximumDistance - DistanceHysteresis,
                _minimumDistance);
            _repositionDistance = _retreatStopDistance;
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
            }

            if (_isRepositioning)
            {
                float repositionRetreatDistance = Mathf.Max(
                    0f, _retreatStartDistance - RepositionRetreatMargin);
                if (distance < repositionRetreatDistance)
                {
                    _isRepositioning = false;
                    _isRetreating = true;
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
                MoveAwayFromCharacter(awayFromCharacter);
                return;
            }

            if (_isApproaching)
            {
                if (distance > _approachStopDistance)
                {
                    MoveAtConstantSpeed(Character.transform.position);
                    return;
                }

                _isApproaching = false;
            }

            if (distance > _maximumDistance)
            {
                _isApproaching = true;
                MoveAtConstantSpeed(Character.transform.position);
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
            _repositionPosition = Character.transform.position +
                                  repositionDirection * _repositionDistance;
            _isRetreating = false;
            _isApproaching = false;
            _isRepositioning = true;
            _isHoldingPosition = false;
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

            Vector3 awayDirection = awayFromCharacter.normalized;
            Vector3 retreatPosition = Character.transform.position +
                                      awayDirection * _retreatStopDistance;
            MoveAtConstantSpeed(retreatPosition);
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

            if (MoveAtConstantSpeed(_repositionPosition))
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

        private bool MoveAtConstantSpeed(Vector3 desiredPosition)
        {
            _isHoldingPosition = false;

            if (SetNavigationDestination(desiredPosition) == false)
            {
                NavMeshAgent.velocity = Vector3.zero;
                return false;
            }

            Vector3 direction = NavMeshAgent.steeringTarget - Enemy.transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < MinimumDirectionSqrMagnitude)
            {
                direction = desiredPosition - Enemy.transform.position;
                direction.y = 0f;
            }

            NavMeshAgent.velocity = direction.sqrMagnitude < MinimumDirectionSqrMagnitude
                ? Vector3.zero
                : direction.normalized * Configuration.Speed;
            return true;
        }

    }
}
