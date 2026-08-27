using UnityEngine;

namespace Features.Enemies.Scripts
{
    public sealed class EnemyStationaryMovementSystem : IEnemyMovementSystem
    {
        private readonly EnemyFacade _enemy;
        private readonly CharacterFacade _character;
        private readonly IEnemyAnimationSystem _animationSystem;

        public bool CanAttack => true;

        public EnemyStationaryMovementSystem(EnemyFacade enemy, CharacterFacade character,
            IEnemyAnimationSystem animationSystem)
        {
            _enemy = enemy;
            _character = character;
            _animationSystem = animationSystem;
        }

        public void Tick()
        {
            if (_enemy.IsDead || _character == null || _enemy.IsStopped)
                return;

            _animationSystem.IdleAnimation();

            if (_enemy.IsAggro == false)
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
