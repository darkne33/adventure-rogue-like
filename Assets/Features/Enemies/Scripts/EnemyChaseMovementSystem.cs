using UnityEngine;
using UnityEngine.AI;

namespace Features.Enemies.Scripts
{
    public sealed class EnemyChaseMovementSystem : EnemyMovementSystemBase
    {
        public EnemyChaseMovementSystem(EnemyFacade enemy, CharacterFacade character,
            EnemyConfiguration configuration, NavMeshAgent navMeshAgent,
            IEnemyAnimationSystem animationSystem)
            : base(enemy, character, configuration, navMeshAgent, animationSystem)
        {
        }

        public override void Tick()
        {
            if (CanMove() == false)
                return;

            Vector3 direction = Character.transform.position - Enemy.transform.position;
            direction.y = 0f;

            if (direction.magnitude <= Configuration.DistanceToStop)
                return;

            MoveTo(Character.transform.position);
        }
    }
}
