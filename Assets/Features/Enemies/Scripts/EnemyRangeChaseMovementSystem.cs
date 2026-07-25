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
        private const float SideStepWeight = 2f;

        private readonly float _minimumDistance;
        private readonly float _maximumDistance;

        private bool _isEvading;
        private float _sideStepDirection;

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

            navMeshAgent.autoBraking = true;
            navMeshAgent.stoppingDistance = 0f;
        }

        public override void Tick()
        {
            if (CanMove() == false)
                return;

            Vector3 awayFromCharacter = Enemy.transform.position - Character.transform.position;
            awayFromCharacter.y = 0f;
            float distance = awayFromCharacter.magnitude;

            if (distance > _maximumDistance)
            {
                _isEvading = false;
                MoveTo(Character.transform.position);
                return;
            }

            if (distance < _minimumDistance)
            {
                EvadeCharacter(awayFromCharacter);
                return;
            }

            _isEvading = false;
            HoldPosition();
        }

        public override void Reset()
        {
            if (NavMeshAgent.isOnNavMesh && NavMeshAgent.hasPath)
                NavMeshAgent.ResetPath();
        }

        private void HoldPosition()
        {
            if (NavMeshAgent.hasPath)
                NavMeshAgent.ResetPath();

            AnimationSystem.IdleAnimation();
        }

        private void EvadeCharacter(Vector3 awayFromCharacter)
        {
            if (awayFromCharacter.sqrMagnitude < MinimumDirectionSqrMagnitude)
            {
                awayFromCharacter = Enemy.transform.forward;
                awayFromCharacter.y = 0f;

                if (awayFromCharacter.sqrMagnitude < MinimumDirectionSqrMagnitude)
                    awayFromCharacter = Vector3.forward;
            }

            if (_isEvading == false)
            {
                _isEvading = true;
                _sideStepDirection = Random.value < 0.5f ? -1f : 1f;
            }

            Vector3 awayDirection = awayFromCharacter.normalized;
            Vector3 sideDirection = Vector3.Cross(Vector3.up, awayDirection) * _sideStepDirection;
            Vector3 evadeDirection = (awayDirection + sideDirection * SideStepWeight).normalized;
            Vector3 evadePosition = Character.transform.position +
                                    evadeDirection * _maximumDistance;

            MoveTo(evadePosition);
        }
    }
}
