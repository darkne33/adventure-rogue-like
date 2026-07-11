using UnityEngine;
using UnityEngine.AI;

namespace Features.Enemies.Scripts
{
    public sealed class EnemyChaseMovementSystem : EnemyMovementSystemBase
    {
        private bool _isFollowingDirectly;

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

            float distance = direction.magnitude;

            if (_isFollowingDirectly)
            {
                float resumeChaseDistance = Mathf.Max(
                    Configuration.CloseFollowDistance,
                    Configuration.ResumeChaseDistance);
                if (distance >= resumeChaseDistance)
                    _isFollowingDirectly = false;
            }
            else if (Configuration.CloseFollowDistance > 0f &&
                     distance <= Configuration.CloseFollowDistance)
            {
                _isFollowingDirectly = true;
            }

            if (distance <= Configuration.DistanceToStop)
                return;

            if (_isFollowingDirectly)
                MoveDirectlyTo(Character.transform.position);
            else
                MoveTo(Character.transform.position);
        }
    }
}
