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
        private const float NavMeshSampleDistance = 4f;
        private const float ImmediateSpeedAcceleration = 1000f;

        private readonly float _minimumDistance;
        private readonly float _maximumDistance;
        private readonly float _retreatStopDistance;
        private readonly float _approachStopDistance;

        private bool _isRetreating;
        private bool _isApproaching;
        private bool _automaticNavigationEnabled;

        public override bool CanAttack => _isRetreating == false;

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
            _approachStopDistance = Mathf.Max(
                _maximumDistance - DistanceHysteresis,
                _minimumDistance);

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

            if (_isApproaching)
            {
                if (distance > _approachStopDistance)
                {
                    SetNavigationDestination(Character.transform.position);
                    return;
                }

                _isApproaching = false;
            }

            if (distance < _minimumDistance)
            {
                _isRetreating = true;
                MoveAwayFromCharacter(awayFromCharacter);
                return;
            }

            if (distance > _maximumDistance)
            {
                _isApproaching = true;
                SetNavigationDestination(Character.transform.position);
                return;
            }

            HoldPosition();
        }

        public override void Reset()
        {
            base.Reset();

            if (NavMeshAgent.isOnNavMesh && NavMeshAgent.hasPath)
                NavMeshAgent.ResetPath();

            _isRetreating = false;
            _isApproaching = false;
        }

        private void HoldPosition()
        {
            if (NavMeshAgent.hasPath)
                NavMeshAgent.ResetPath();

            AnimationSystem.IdleAnimation();
        }

        private void MoveAwayFromCharacter(Vector3 awayFromCharacter)
        {
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
            SetNavigationDestination(retreatPosition);
        }

        private void EnableAutomaticNavigation()
        {
            if (_automaticNavigationEnabled)
                return;

            NavMeshAgent.nextPosition = Enemy.transform.position;
            NavMeshAgent.updatePosition = true;
            NavMeshAgent.updateRotation = true;
            NavMeshAgent.acceleration = ImmediateSpeedAcceleration;
            _automaticNavigationEnabled = true;
        }

        private void SetNavigationDestination(Vector3 desiredPosition)
        {
            if (NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit,
                    NavMeshSampleDistance, NavMesh.AllAreas))
                NavMeshAgent.SetDestination(hit.position);
        }
    }
}
