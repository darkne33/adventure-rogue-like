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

        public virtual bool CanAttack => true;

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

        public virtual void OnAttackFinished()
        {
        }

        public virtual void Reset()
        {
        }

        protected bool CanMove()
        {
            if (Character == null || NavMeshAgent.isStopped ||
                NavMeshAgent.isOnNavMesh == false || Enemy.IsDead)
                return false;

            if (Enemy.IsAggro == false)
            {
                if (IsCharacterInsideAggroRange() == false)
                {
                    ApproachCharacter();
                    return false;
                }

                ActivateAggro();
                AnimationSystem.IdleAnimation();
                return false;
            }

            AnimationSystem.RunAnimation();
            return true;
        }

        private bool IsCharacterInsideAggroRange()
        {
            if (Character == null)
                return false;

            Vector3 offset = Character.transform.position - Enemy.transform.position;
            offset.y = 0f;
            float aggroRange = Mathf.Max(0.1f, Configuration.AggroRange);
            return offset.sqrMagnitude <= aggroRange * aggroRange;
        }

        private void ActivateAggro()
        {
            Enemy.ActivateAggro();

            if (NavMeshAgent.hasPath)
                NavMeshAgent.ResetPath();
        }

        private void ApproachCharacter()
        {
            AnimationSystem.RunAnimation();
            MoveTo(Character.transform.position);
        }

        protected void MoveTo(Vector3 desiredPosition)
        {
            if (SetNavigationDestination(desiredPosition) == false)
                return;

            UpdateManualNavigationPosition();
        }

        protected void MoveDirectlyTo(Vector3 desiredPosition)
        {
            NavMeshAgent.SetDestination(desiredPosition);
            UpdateManualNavigationPosition();
        }

        protected bool SetNavigationDestination(Vector3 desiredPosition)
        {
            return NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit,
                       NavMeshSampleDistance, NavMesh.AllAreas) &&
                   NavMeshAgent.SetDestination(hit.position);
        }

        protected void UpdateManualNavigationPosition()
        {
            if (NavMeshAgent.updatePosition)
                return;

            Vector3 currentPosition = Enemy.transform.position;
            Vector3 navigationPosition = NavMeshAgent.nextPosition;

            // Keep the visible enemy on the agent's path instead of lerping
            // across NavMesh corners. Vertical movement remains physics-driven.
            currentPosition.x = navigationPosition.x;
            currentPosition.z = navigationPosition.z;
            Enemy.transform.position = currentPosition;
        }
    }
}
