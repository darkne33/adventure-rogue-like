using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    public class EnemyDashAttackSystem : IEnemyDamageSystem
    {
        private readonly CharacterFacade _characterFacade;
        private readonly EnemyConfiguration _enemyConfiguration;
        private readonly Transform _enemyTarget;
        private readonly EnemyFacade _enemyFacade;
        
        private float _cooldown;
        private float _distanceExecuteDamage;

        public EnemyDashAttackSystem(CharacterFacade characterFacade, EnemyConfiguration enemyConfiguration, Transform enemyTarget, EnemyFacade enemyFacade)
        {
            _characterFacade = characterFacade;
            _enemyConfiguration = enemyConfiguration;
            _enemyTarget = enemyTarget;
            _enemyFacade = enemyFacade;
        }

        public void Initialize()
        {
            _distanceExecuteDamage = _enemyConfiguration.DamageRange;
            _cooldown = _enemyConfiguration.DamageCooldown;
        }
        
        public UniTask Execute(CancellationToken cancellationToken)
        {
            _enemyFacade.SetStop(true);
            
            
            
            _enemyFacade.SetStop(false);
            return UniTask.CompletedTask;
        }

        public async UniTask Tick(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var distanceToCharacter = Vector3.Distance(_characterFacade.transform.position, _enemyTarget.position);
                _cooldown -= Time.deltaTime;
                if (_cooldown <= 0 && distanceToCharacter <= _distanceExecuteDamage)
                {
                    await Execute(cancellationToken);
                    _cooldown = _enemyConfiguration.DamageCooldown;
                }

                await UniTask.Yield(cancellationToken);
            }
        }
    }
}