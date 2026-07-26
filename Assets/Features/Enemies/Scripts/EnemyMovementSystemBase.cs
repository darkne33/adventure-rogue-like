using UnityEngine;
using UnityEngine.AI;

namespace Features.Enemies.Scripts
{
    public abstract class EnemyMovementSystemBase : IEnemyMovementSystem
    {
        private const int WanderDestinationSearchAttempts = 8;
        private const float NavMeshSampleDistance = 4f;
        private const float WanderDestinationReachedDistance = 0.4f;
        private const float WanderDestinationRetryDelay = 0.5f;
        private const float MinimumWanderDirectionSqrMagnitude = 0.01f;

        protected readonly EnemyFacade Enemy;
        protected readonly CharacterFacade Character;
        protected readonly EnemyConfiguration Configuration;
        protected readonly NavMeshAgent NavMeshAgent;
        protected readonly IEnemyAnimationSystem AnimationSystem;

        public virtual bool CanAttack => true;

        private readonly NavMeshPath _wanderPath = new();
        private bool _hasWanderDestination;
        private float _nextWanderAttemptTime;

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
            if (Enemy.IsAggro)
                return;

            _hasWanderDestination = false;
            _nextWanderAttemptTime = Time.time;
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
                    Wander();
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
            _hasWanderDestination = false;

            if (NavMeshAgent.hasPath)
                NavMeshAgent.ResetPath();
        }

        private void Wander()
        {
            if (ShouldSelectWanderDestination())
                SelectWanderDestination();

            if (_hasWanderDestination == false)
            {
                AnimationSystem.IdleAnimation();
                return;
            }

            AnimationSystem.RunAnimation();
            Enemy.transform.position = Vector3.Lerp(
                Enemy.transform.position,
                NavMeshAgent.nextPosition,
                Time.deltaTime * NavMeshAgent.speed);
        }

        private bool ShouldSelectWanderDestination()
        {
            if (_hasWanderDestination == false)
                return Time.time >= _nextWanderAttemptTime;

            if (NavMeshAgent.pathPending)
                return false;

            if (NavMeshAgent.pathStatus != NavMeshPathStatus.PathComplete)
                return true;

            float reachedDistance = Mathf.Max(
                WanderDestinationReachedDistance,
                NavMeshAgent.stoppingDistance);
            return NavMeshAgent.remainingDistance <= reachedDistance;
        }

        private void SelectWanderDestination()
        {
            float wanderRadius = Mathf.Max(0f, Configuration.WanderRadius);
            if (wanderRadius <= 0f)
            {
                _hasWanderDestination = false;
                _nextWanderAttemptTime = float.PositiveInfinity;
                return;
            }

            for (int attempt = 0; attempt < WanderDestinationSearchAttempts; attempt++)
            {
                Vector2 randomOffset = Random.insideUnitCircle;
                if (randomOffset.sqrMagnitude < MinimumWanderDirectionSqrMagnitude)
                    continue;

                Vector3 desiredPosition = Enemy.transform.position +
                                          new Vector3(randomOffset.x, 0f, randomOffset.y) * wanderRadius;

                if (NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, NavMeshSampleDistance,
                        NavMesh.AllAreas) == false ||
                    NavMeshAgent.CalculatePath(hit.position, _wanderPath) == false ||
                    _wanderPath.status != NavMeshPathStatus.PathComplete ||
                    NavMeshAgent.SetDestination(hit.position) == false)
                {
                    continue;
                }

                _hasWanderDestination = true;
                return;
            }

            _hasWanderDestination = false;
            _nextWanderAttemptTime = Time.time + WanderDestinationRetryDelay;
        }

        protected void MoveTo(Vector3 desiredPosition)
        {
            if (NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, NavMeshSampleDistance,
                    NavMesh.AllAreas) == false)
                return;

            MoveDirectlyTo(hit.position);
        }

        protected void MoveDirectlyTo(Vector3 desiredPosition)
        {
            NavMeshAgent.SetDestination(desiredPosition);
            Enemy.transform.position = Vector3.Lerp(
                Enemy.transform.position,
                NavMeshAgent.nextPosition,
                Time.deltaTime * NavMeshAgent.speed);
        }
    }
}
