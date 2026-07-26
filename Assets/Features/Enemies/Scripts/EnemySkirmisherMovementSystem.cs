using UnityEngine;
using UnityEngine.AI;

namespace Features.Enemies.Scripts
{
    public sealed class EnemySkirmisherMovementSystem : EnemyMovementSystemBase
    {
        private enum MovementState
        {
            Approach,
            Retreat,
            Orbit
        }

        private const float CloseDistance = 5f;
        private const float FarDistance = 12f;
        private const float OrbitRadius = 8f;
        private const float OrbitOffset = 6f;
        private const float RetreatDistance = 11f;

        private MovementState _state;
        private float _decisionTimer;
        private float _orbitDirection = 1f;

        public EnemySkirmisherMovementSystem(EnemyFacade enemy, CharacterFacade character,
            EnemyConfiguration configuration, NavMeshAgent navMeshAgent,
            IEnemyAnimationSystem animationSystem)
            : base(enemy, character, configuration, navMeshAgent, animationSystem)
        {
        }

        public override void Tick()
        {
            if (CanMove() == false)
                return;

            Vector3 awayFromCharacter = Enemy.transform.position - Character.transform.position;
            awayFromCharacter.y = 0f;
            float distance = awayFromCharacter.magnitude;

            if (awayFromCharacter.sqrMagnitude < 0.001f)
                awayFromCharacter = Enemy.transform.forward;
            else
                awayFromCharacter.Normalize();

            _decisionTimer -= Time.fixedDeltaTime;
            if (_decisionTimer <= 0f)
                SelectState(distance);

            Vector3 targetPosition;
            switch (_state)
            {
                case MovementState.Retreat:
                    targetPosition = Character.transform.position + awayFromCharacter * RetreatDistance;
                    break;
                case MovementState.Orbit:
                    Vector3 tangent = Vector3.Cross(Vector3.up, awayFromCharacter) * _orbitDirection;
                    targetPosition = Character.transform.position +
                                     awayFromCharacter * OrbitRadius +
                                     tangent * OrbitOffset;
                    break;
                default:
                    targetPosition = Character.transform.position;
                    break;
            }

            MoveTo(targetPosition);
        }

        public override void Reset()
        {
            base.Reset();
            _decisionTimer = 0f;
        }

        private void SelectState(float distance)
        {
            _decisionTimer = Random.Range(0.6f, 1.5f);

            if (distance <= CloseDistance)
            {
                _state = MovementState.Retreat;
                return;
            }

            if (distance >= FarDistance)
            {
                _state = MovementState.Approach;
                return;
            }

            float choice = Random.value;
            if (choice < 0.2f)
            {
                _state = MovementState.Retreat;
            }
            else if (choice < 0.45f)
            {
                _state = MovementState.Approach;
            }
            else
            {
                _state = MovementState.Orbit;
                _orbitDirection = Random.value < 0.5f ? -1f : 1f;
            }
        }
    }
}
