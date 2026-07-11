using UnityEngine;
using UnityEngine.AI;

namespace Features.Enemies.Scripts
{
    /// <summary>
    /// Continuously updates the NavMesh destination with the character position.
    /// </summary>
    public sealed class EnemyAggressiveMovementSystem : EnemyMovementSystemBase
    {
        private bool _automaticNavigationEnabled;

        public EnemyAggressiveMovementSystem(EnemyFacade enemy, CharacterFacade character,
            EnemyConfiguration configuration, NavMeshAgent navMeshAgent,
            IEnemyAnimationSystem animationSystem)
            : base(enemy, character, configuration, navMeshAgent, animationSystem)
        {
            navMeshAgent.autoBraking = false;
            navMeshAgent.stoppingDistance = 0f;
        }

        public override void Tick()
        {
            if (Character == null)
                return;

            if (_automaticNavigationEnabled == false)
            {
                if (NavMeshAgent.isStopped)
                    return;

                EnableAutomaticNavigation();
            }

            RotateTowardsCharacter();

            if (NavMeshAgent.isStopped)
                return;

            AnimationSystem.RunAnimation();
            NavMeshAgent.SetDestination(Character.transform.position);
        }

        private void RotateTowardsCharacter()
        {
            Vector3 direction = Character.transform.position - Enemy.transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            Enemy.transform.rotation = Quaternion.RotateTowards(
                Enemy.transform.rotation,
                targetRotation,
                Configuration.RotationSpeed * Time.fixedDeltaTime);
        }

        private void EnableAutomaticNavigation()
        {
            if (_automaticNavigationEnabled)
                return;

            NavMeshAgent.nextPosition = Enemy.transform.position;
            NavMeshAgent.updatePosition = true;
            NavMeshAgent.updateRotation = false;
            _automaticNavigationEnabled = true;
        }
    }
}
