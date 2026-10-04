using UnityEngine;
using UnityEngine.AI;

namespace Features.Enemies.Scripts
{
    // Shared by dash and jump attacks so their existing ground movement stays the same.
    public sealed class EnemyPursuitMovementSystem : IEnemyMovementSystem
    {
        private const float NavigationSampleDistance = 4f;

        private readonly EnemyFacade _enemy;
        private readonly CharacterFacade _character;
        private readonly EnemyConfiguration _configuration;
        private readonly Rigidbody _body;
        private readonly NavMeshAgent _agent;
        private readonly EnemyDashSkirmisherMovement _skirmisherMovement;
        private readonly float _stopDistance;

        public bool CanAttack => true;

        public EnemyPursuitMovementSystem(EnemyFacade enemy, CharacterFacade character,
            EnemyConfiguration configuration, NavMeshAgent agent, IEnemiesProvider enemiesProvider)
        {
            _enemy = enemy;
            _character = character;
            _configuration = configuration;
            _body = enemy.Rigidbody;
            _agent = agent;

            float attackRange = Mathf.Min(Mathf.Max(0f, configuration.DamageRange),
                Mathf.Max(0f, configuration.DashDistance));
            _stopDistance = Mathf.Clamp(configuration.DistanceToStop > 0f
                ? configuration.DistanceToStop : attackRange * 0.8f, 0f, attackRange);

            _agent.updatePosition = false;
            _agent.updateRotation = false;
            _agent.autoBraking = true;
            _agent.stoppingDistance = _stopDistance;
            if (configuration.EnemyMovementType == EnemyMovementType.Skirmisher)
            {
                _skirmisherMovement = new EnemyDashSkirmisherMovement(enemy,
                    character, enemiesProvider, agent, attackRange);
                _agent.stoppingDistance = 0.25f;
            }
            _body.constraints |= RigidbodyConstraints.FreezeRotation;
        }

        public void Tick()
        {
            if (_body == null || _enemy.IsDead || _character == null)
                return;

            if (_enemy.IsStopped || _agent.isOnNavMesh == false)
            {
                StopHorizontalMovement();
                _enemy.AnimationSystem.IdleAnimation();
                return;
            }

            Vector3 toCharacter = _character.transform.position - _body.position;
            toCharacter.y = 0f;
            if (_enemy.IsAggro == false &&
                toCharacter.sqrMagnitude <= Mathf.Pow(Mathf.Max(0.1f, _configuration.AggroRange), 2f))
            {
                _enemy.ActivateAggro();
                StopHorizontalMovement();
                return;
            }

            _agent.nextPosition = _body.position;
            bool isSkirmishing = _skirmisherMovement != null && _enemy.IsAggro;
            if (isSkirmishing == false && _enemy.IsAggro &&
                toCharacter.sqrMagnitude <= _stopDistance * _stopDistance)
            {
                if (_agent.hasPath)
                    _agent.ResetPath();
                StopHorizontalMovement();
                RotateBodyTowards(toCharacter);
                _enemy.AnimationSystem.IdleAnimation();
                return;
            }

            Vector3 destination = _character.transform.position;
            if (isSkirmishing && _skirmisherMovement.TryGetDestination(
                    Time.fixedDeltaTime * _enemy.RelicTimeScale, out destination) == false)
            {
                if (_agent.hasPath)
                    _agent.ResetPath();
                StopHorizontalMovement();
                RotateBodyTowards(toCharacter);
                _enemy.AnimationSystem.IdleAnimation();
                return;
            }

            if (NavMesh.SamplePosition(destination, out NavMeshHit hit,
                    NavigationSampleDistance, _agent.areaMask) == false ||
                _agent.SetDestination(hit.position) == false)
            {
                StopHorizontalMovement();
                _enemy.AnimationSystem.IdleAnimation();
                return;
            }

            Vector3 velocity = _agent.desiredVelocity;
            velocity.y = 0f;
            velocity = Vector3.ClampMagnitude(velocity, _agent.speed);
            _body.linearVelocity = new Vector3(velocity.x, _body.linearVelocity.y, velocity.z);
            RotateBodyTowards(velocity);
            if (velocity.sqrMagnitude > 0.001f)
                _enemy.AnimationSystem.RunAnimation();
            else
                _enemy.AnimationSystem.IdleAnimation();
        }

        public void Reset()
        {
            if (_agent == null || _agent.isOnNavMesh == false)
                return;

            _skirmisherMovement?.Reset();
            _agent.nextPosition = _body.position;
            if (_agent.hasPath)
                _agent.ResetPath();
        }

        public void OnAttackFinished()
        {
        }

        private void StopHorizontalMovement()
        {
            Vector3 velocity = _body.linearVelocity;
            _body.linearVelocity = new Vector3(0f, velocity.y, 0f);
            _body.angularVelocity = Vector3.zero;
        }

        private void RotateBodyTowards(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.001f)
                return;
            _body.angularVelocity = Vector3.zero;
            _body.MoveRotation(Quaternion.RotateTowards(_body.rotation,
                Quaternion.LookRotation(direction.normalized, Vector3.up),
                Mathf.Max(0f, _configuration.RotationSpeed) * Time.fixedDeltaTime));
        }
    }
}
