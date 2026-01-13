using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    public class EnemyDamageMeleeSystem : IEnemyDamageSystem
    {
        private readonly CharacterFacade _characterFacade;
        private readonly EnemyConfiguration _enemyConfiguration;
        private readonly Transform _enemyTarget;
        private readonly EnemyFacade _enemyFacade;

        private float _cooldown;
        private readonly float _distanceExecuteDamage;

        public EnemyDamageMeleeSystem(EnemyFacade enemyFacade, CharacterFacade characterFacade,
            EnemyConfiguration enemyConfiguration, Transform enemyTarget)
        {
            _enemyFacade = enemyFacade;
            _characterFacade = characterFacade;
            _enemyConfiguration = enemyConfiguration;
            _enemyTarget = enemyTarget;

            _distanceExecuteDamage = _enemyConfiguration.DamageRange;
        }

        public void Execute()
        {
            _characterFacade.CharacterHealthSystem.GetDamage(_enemyConfiguration.Damage);

            Vector3 pushDirection = _characterFacade.transform.position - _enemyTarget.position;
            pushDirection.y = 0f;
            pushDirection.Normalize();
            Vector3 force = pushDirection;
            _characterFacade.Rigidbody.AddForce(force * 10f, ForceMode.Impulse);
            
            _enemyFacade.StartDelayMovementTimer(1).Forget();
        }

        public void Tick()
        {
            var distanceToCharacter = Vector3.Distance(_characterFacade.transform.position, _enemyTarget.position);
            _cooldown -= Time.deltaTime;
            if (_cooldown <= 0 && distanceToCharacter <= _distanceExecuteDamage)
            {
                Execute();
                _cooldown = _enemyConfiguration.DamageCooldown;
            }
        }

        public void Initialize()
        {
            _cooldown = _enemyConfiguration.DamageCooldown;
        }
    }
}