using UnityEngine;
using UnityEngine.AI;

namespace Features.Enemies.Scripts
{
    /// <summary>
    /// Pursues the character directly and prevents the agent from orbiting
    /// around the target when it reaches melee range.
    /// </summary>
    public sealed class EnemyAggressiveChaseMovementSystem : EnemyMovementSystemBase
    {
        private const float MeleeStopRangeMultiplier = 0.8f;
        private const int AggressiveAvoidancePriority = 20;

        private readonly float _stopDistance;
        private bool _automaticNavigationEnabled;

        public EnemyAggressiveChaseMovementSystem(EnemyFacade enemy, CharacterFacade character,
            EnemyConfiguration configuration, NavMeshAgent navMeshAgent,
            IEnemyAnimationSystem animationSystem)
            : base(enemy, character, configuration, navMeshAgent, animationSystem)
        {
            _stopDistance = Mathf.Max(configuration.DistanceToStop,
                configuration.DamageRange * MeleeStopRangeMultiplier);

            navMeshAgent.autoBraking = true;
            navMeshAgent.stoppingDistance = _stopDistance;
            navMeshAgent.avoidancePriority = Mathf.Min(navMeshAgent.avoidancePriority,
                AggressiveAvoidancePriority);
        }

        public override void Tick()
        {
            if (CanMove() == false)
                return;

            EnableAutomaticNavigation();

            Vector3 toCharacter = Character.transform.position - Enemy.transform.position;
            toCharacter.y = 0f;

            if (toCharacter.sqrMagnitude <= _stopDistance * _stopDistance)
            {
                NavMeshAgent.SetDestination(Character.transform.position);
                AnimationSystem.IdleAnimation();
                return;
            }

            MoveTo(Character.transform.position);
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
    }
}
