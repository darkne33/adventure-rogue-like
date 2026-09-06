using UnityEngine;
using UnityEngine.AI;

namespace Features.Enemies.Scripts
{
    // Selects positions between attacks; EnemyDashAttackSystem still owns body movement.
    public sealed class EnemyDashSkirmisherMovement
    {
        private enum MovementState
        {
            Approach,
            Retreat,
            Orbit
        }

        private const float PositionSampleDistance = 2f;
        private const float MinimumTravelDistance = 1f;

        private readonly EnemyFacade _enemy;
        private readonly CharacterFacade _character;
        private readonly IEnemiesProvider _enemiesProvider;
        private readonly NavMeshAgent _agent;
        private readonly NavMeshPath _candidatePath = new();
        private readonly float _attackRange;
        private readonly float _crowdRadius;

        private float _decisionTimer;
        private float _orbitDirection;
        private bool _hasDestination;
        private Vector3 _destination;

        public EnemyDashSkirmisherMovement(EnemyFacade enemy, CharacterFacade character,
            IEnemiesProvider enemiesProvider, NavMeshAgent agent, float attackRange)
        {
            _enemy = enemy;
            _character = character;
            _enemiesProvider = enemiesProvider;
            _agent = agent;
            _attackRange = Mathf.Max(0.1f, attackRange);
            _crowdRadius = Mathf.Max(3f, agent.radius * 2.5f);
            _orbitDirection = Random.value < 0.5f ? -1f : 1f;
        }

        public bool TryGetDestination(float deltaTime, out Vector3 destination)
        {
            Vector3 fromCharacter = _enemy.Rigidbody.position - _character.transform.position;
            fromCharacter.y = 0f;
            if (_hasDestination && fromCharacter.sqrMagnitude <= _attackRange * _attackRange * 0.25f)
            {
                Vector3 destinationOffset = _destination - _character.transform.position;
                destinationOffset.y = 0f;
                if (destinationOffset.magnitude > fromCharacter.magnitude + 0.01f)
                    _decisionTimer = 0f;
            }

            _decisionTimer -= deltaTime;
            if (_decisionTimer <= 0f)
            {
                _decisionTimer = Random.Range(0.6f, 1.5f);
                SelectDestination();
            }

            destination = _destination;
            return _hasDestination;
        }

        public void Reset()
        {
            _decisionTimer = 0f;
            _hasDestination = false;
        }

        private void SelectDestination()
        {
            Vector3 origin = _enemy.Rigidbody.position;
            Vector3 characterPosition = _character.transform.position;
            Vector3 awayFromCharacter = origin - characterPosition;
            awayFromCharacter.y = 0f;
            float distance = awayFromCharacter.magnitude;
            awayFromCharacter = distance > 0.001f
                ? awayFromCharacter / distance
                : Vector3.forward;

            MovementState state = SelectState(distance);
            bool isClose = distance <= _attackRange * 0.5f;
            float radius = state == MovementState.Retreat
                ? Mathf.Min(_attackRange * 0.95f, Mathf.Max(_attackRange * 0.8f, distance + 2f))
                : _attackRange * 0.8f;
            if (isClose)
                radius = Mathf.Min(radius, distance);
            float angleStep = state == MovementState.Orbit ? 45f : 25f;
            if (Random.value < 0.25f)
                _orbitDirection = -_orbitDirection;

            _hasDestination = false;
            float bestScore = float.NegativeInfinity;
            float bestDirection = _orbitDirection;

            // Try both flanks, a wider detour on either side, and a radial fallback.
            for (int i = 0; i < 5; i++)
            {
                float angle = i == 4 ? 0f : angleStep * (i < 2 ? 1f : 2f) *
                    (i % 2 == 0 ? _orbitDirection : -_orbitDirection);
                Vector3 desired = characterPosition +
                    Quaternion.Euler(0f, angle, 0f) * awayFromCharacter * radius;
                if (NavMesh.SamplePosition(desired, out NavMeshHit hit,
                        PositionSampleDistance, _agent.areaMask) == false)
                    continue;

                Vector3 fromCharacter = hit.position - characterPosition;
                fromCharacter.y = 0f;
                float candidateDistance = fromCharacter.magnitude;
                if (candidateDistance > _attackRange ||
                    (isClose && candidateDistance > distance + 0.01f) ||
                    (state == MovementState.Retreat && candidateDistance <= distance))
                    continue;

                Vector3 travel = hit.position - origin;
                travel.y = 0f;
                if (travel.sqrMagnitude < MinimumTravelDistance * MinimumTravelDistance ||
                    _agent.CalculatePath(hit.position, _candidatePath) == false ||
                    _candidatePath.status != NavMeshPathStatus.PathComplete)
                    continue;

                float score = -GetCrowding(hit.position) * 5f -
                    GetCrowding((origin + hit.position) * 0.5f) * 2f - travel.magnitude * 0.1f;
                if (i == 0)
                    score += 0.5f;
                if (state == MovementState.Orbit && i == 4)
                    score -= 1f;
                if (score <= bestScore)
                    continue;

                bestScore = score;
                _destination = hit.position;
                _hasDestination = true;
                bestDirection = angle == 0f ? _orbitDirection : Mathf.Sign(angle);
            }

            _orbitDirection = bestDirection;
        }

        private MovementState SelectState(float distance)
        {
            if (distance <= _attackRange * 0.5f)
                return MovementState.Orbit;
            if (distance >= _attackRange * 0.95f)
                return MovementState.Approach;

            float choice = Random.value;
            if (choice < 0.2f)
                return MovementState.Retreat;
            return choice < 0.45f ? MovementState.Approach : MovementState.Orbit;
        }

        private float GetCrowding(Vector3 position)
        {
            float crowding = 0f;
            float radiusSquared = _crowdRadius * _crowdRadius;
            var enemies = _enemiesProvider.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyFacade other = enemies[i];
                if (other == null || other == _enemy || other.IsDead || other.isActiveAndEnabled == false)
                    continue;

                Vector3 offset = other.transform.position - position;
                offset.y = 0f;
                if (offset.sqrMagnitude < radiusSquared)
                    crowding += 1f - offset.sqrMagnitude / radiusSquared;
            }

            return crowding;
        }
    }
}
