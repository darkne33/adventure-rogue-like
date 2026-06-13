using UnityEngine;
using UnityEngine.AI;

namespace Features.Enemies.Scripts
{
    public abstract class EnemyMovementSystemBase : IEnemyMovementSystem
    {
        private const float NavMeshSampleDistance = 4f;

        protected readonly EnemyFacade Enemy;
        protected readonly CharacterFacade Character;
        protected readonly EnemyConfiguration Configuration;
        protected readonly NavMeshAgent NavMeshAgent;
        protected readonly IEnemyAnimationSystem AnimationSystem;

        protected EnemyMovementSystemBase(EnemyFacade enemy, CharacterFacade character,
            EnemyConfiguration configuration, NavMeshAgent navMeshAgent,
            IEnemyAnimationSystem animationSystem)
        {
            Enemy = enemy;
            Character = character;
            Configuration = configuration;
            NavMeshAgent = navMeshAgent;
            AnimationSystem = animationSystem;
        }

        public abstract void Tick();

        public virtual void Reset()
        {
        }

        protected bool CanMove()
        {
            if (Character == null || NavMeshAgent.isStopped)
                return false;

            AnimationSystem.RunAnimation();
            return true;
        }

        protected void MoveTo(Vector3 desiredPosition)
        {
            if (NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, NavMeshSampleDistance,
                    NavMesh.AllAreas) == false)
                return;

            NavMeshAgent.SetDestination(hit.position);
            Enemy.transform.position = Vector3.Lerp(
                Enemy.transform.position,
                NavMeshAgent.nextPosition,
                Time.deltaTime * NavMeshAgent.speed);
        }
    }
}
