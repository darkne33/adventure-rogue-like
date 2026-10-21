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
            PreparePhysicsMovement();

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
            Rigidbody rigidbody = Enemy.Rigidbody;

            // Kinematic enemies do not need physics to own their position. Let
            // the agent update them every rendered frame instead of copying its
            // position from FixedUpdate, which makes fast enemies visibly step.
            // This is enabled on the first movement tick so the spawn-rise tween
            // can finish while automatic position updates are still disabled.
            if (rigidbody == null || rigidbody.isKinematic)
            {
                if (NavMeshAgent.updatePosition)
                    return;

                NavMeshAgent.nextPosition = Enemy.transform.position;
                NavMeshAgent.updatePosition = true;
                return;
            }

            // Navigation supplies steering; physics owns the body's pose so
            // Rigidbody interpolation can smooth it between fixed updates.
            Vector3 velocity = NavMeshAgent.desiredVelocity;
            velocity.y = 0f;
            velocity = Vector3.ClampMagnitude(velocity, NavMeshAgent.speed);
            rigidbody.linearVelocity = new Vector3(
                velocity.x, rigidbody.linearVelocity.y, velocity.z);
            RotateTowardsDirection(velocity);
        }

        protected void RotateTowardsDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            float rotationStep = Mathf.Max(0f, Configuration.RotationSpeed) * Time.fixedDeltaTime;
            Rigidbody rigidbody = Enemy.Rigidbody;
            if (rigidbody != null && rigidbody.isKinematic == false)
            {
                rigidbody.MoveRotation(Quaternion.RotateTowards(
                    rigidbody.rotation, targetRotation, rotationStep));
                return;
            }

            Enemy.transform.rotation = Quaternion.RotateTowards(
                Enemy.transform.rotation, targetRotation, rotationStep);
        }

        protected void StopPhysicsMovement()
        {
            Rigidbody rigidbody = Enemy.Rigidbody;
            if (rigidbody == null || rigidbody.isKinematic)
                return;

            Vector3 velocity = rigidbody.linearVelocity;
            rigidbody.linearVelocity = new Vector3(0f, velocity.y, 0f);
            rigidbody.angularVelocity = Vector3.zero;
        }

        private void PreparePhysicsMovement()
        {
            Rigidbody rigidbody = Enemy.Rigidbody;
            if (rigidbody == null || rigidbody.isKinematic)
                return;

            NavMeshAgent.updatePosition = false;
            NavMeshAgent.updateRotation = false;

            // Every tick starts stopped. A movement decision below supplies a
            // new velocity; attack, aggro and arrival branches must not coast.
            StopPhysicsMovement();

            if (NavMeshAgent.isActiveAndEnabled && NavMeshAgent.isOnNavMesh)
                NavMeshAgent.nextPosition = rigidbody.position;
        }
    }
}
