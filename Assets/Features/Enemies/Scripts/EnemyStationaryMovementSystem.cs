using UnityEngine;

namespace Features.Enemies.Scripts
{
    public sealed class EnemyStationaryMovementSystem : IEnemyMovementSystem
    {
        private readonly EnemyFacade _enemy;
        private readonly CharacterFacade _character;
        private readonly EnemyConfiguration _configuration;
        private readonly IEnemyAnimationSystem _animationSystem;

        public bool CanAttack => true;

        public EnemyStationaryMovementSystem(EnemyFacade enemy, CharacterFacade character,
            EnemyConfiguration configuration, IEnemyAnimationSystem animationSystem)
        {
            _enemy = enemy;
            _character = character;
            _configuration = configuration;
            _animationSystem = animationSystem;
        }

        public void Tick()
        {
            if (_enemy.IsDead || _character == null)
                return;

            _animationSystem.IdleAnimation();

            if (_enemy.IsAggro)
                return;

            Vector3 offset = _character.transform.position - _enemy.transform.position;
            offset.y = 0f;
            float aggroRange = Mathf.Max(0.1f, _configuration.AggroRange);

            if (offset.sqrMagnitude <= aggroRange * aggroRange)
                _enemy.ActivateAggro();
        }

        public void Reset()
        {
        }

        public void OnAttackFinished()
        {
        }
    }
}
