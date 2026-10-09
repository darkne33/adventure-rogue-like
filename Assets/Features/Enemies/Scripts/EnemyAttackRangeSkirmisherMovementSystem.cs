using UnityEngine;
using UnityEngine.AI;

namespace Features.Enemies.Scripts
{
    public sealed class EnemyAttackRangeSkirmisherMovementSystem : EnemyMovementSystemBase
    {
        private readonly EnemyDashSkirmisherMovement _positionSelector;

        public EnemyAttackRangeSkirmisherMovementSystem(EnemyFacade enemy, CharacterFacade character,
            EnemyConfiguration configuration, NavMeshAgent navMeshAgent,
            IEnemyAnimationSystem animationSystem, IEnemiesProvider enemiesProvider)
            : base(enemy, character, configuration, navMeshAgent, animationSystem)
        {
            // Reuse the Head enemy's position selection with this enemy's attack range.
            _positionSelector = new EnemyDashSkirmisherMovement(
                enemy, character, enemiesProvider, navMeshAgent, configuration.DamageRange);
            navMeshAgent.autoBraking = true;
            navMeshAgent.stoppingDistance = 0.25f;
        }

        public override void Tick()
        {
            if (CanMove() == false)
                return;

            if (_positionSelector.TryGetDestination(
                    Time.fixedDeltaTime * Enemy.RelicTimeScale, out Vector3 destination))
            {
                MoveTo(destination);
                return;
            }

            if (NavMeshAgent.hasPath)
                NavMeshAgent.ResetPath();

            RotateTowardsCharacter();
            AnimationSystem.IdleAnimation();
        }

        public override void Reset() =>
            _positionSelector.Reset();

        private void RotateTowardsCharacter()
        {
            Vector3 direction = Character.transform.position - Enemy.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.001f)
                return;

            Enemy.transform.rotation = Quaternion.RotateTowards(Enemy.transform.rotation,
                Quaternion.LookRotation(direction.normalized, Vector3.up),
                Configuration.RotationSpeed * Time.fixedDeltaTime);
        }
    }
}
