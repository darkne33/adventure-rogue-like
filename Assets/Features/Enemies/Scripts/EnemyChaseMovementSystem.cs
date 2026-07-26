using UnityEngine;
using UnityEngine.AI;

namespace Features.Enemies.Scripts
{
    public sealed class EnemyChaseMovementSystem : EnemyMovementSystemBase
    {
        private const float MeleeStopRangeMultiplier = 0.8f;

        private readonly float _stopDistance;
        private bool _isFollowingDirectly;

        public EnemyChaseMovementSystem(EnemyFacade enemy, CharacterFacade character,
            EnemyConfiguration configuration, NavMeshAgent navMeshAgent,
            IEnemyAnimationSystem animationSystem)
            : base(enemy, character, configuration, navMeshAgent, animationSystem)
        {
            _stopDistance = Mathf.Max(
                configuration.DistanceToStop,
                configuration.DamageRange * MeleeStopRangeMultiplier);

            navMeshAgent.autoBraking = true;
            navMeshAgent.stoppingDistance = _stopDistance;
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

            if (distance <= _stopDistance)
            {
                if (NavMeshAgent.hasPath)
                    NavMeshAgent.ResetPath();

                RotateTowardsCharacter(direction);
                AnimationSystem.IdleAnimation();
                return;
            }

            if (_isFollowingDirectly)
                MoveDirectlyTo(Character.transform.position);
            else
                MoveTo(Character.transform.position);
        }

        private void RotateTowardsCharacter(Vector3 direction)
        {
            if (direction.sqrMagnitude <= 0.001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            Enemy.transform.rotation = Quaternion.RotateTowards(
                Enemy.transform.rotation,
                targetRotation,
                Configuration.RotationSpeed * Time.fixedDeltaTime);
        }
    }
}
