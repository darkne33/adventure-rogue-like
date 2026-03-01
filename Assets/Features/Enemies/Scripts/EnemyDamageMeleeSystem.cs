using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    public class EnemyDamageMeleeSystem : IEnemyDamageSystem
    {
        private readonly CharacterFacade _characterFacade;
        private readonly EnemyConfiguration _enemyConfiguration;
        private readonly EnemyFacade _enemyFacade;

        private float _cooldown;
        private readonly float _distanceExecuteDamage;

        public EnemyDamageMeleeSystem(EnemyFacade enemyFacade, CharacterFacade characterFacade,
            EnemyConfiguration enemyConfiguration)
        {
            _enemyFacade = enemyFacade;
            _characterFacade = characterFacade;
            _enemyConfiguration = enemyConfiguration;

            _distanceExecuteDamage = _enemyConfiguration.DamageRange;
        }

        public async UniTask Execute(CancellationToken cancellationToken)
        {
            _enemyFacade.StartDelayMovementTimer(1).Forget();
            _enemyFacade.AnimationSystem.AttackAnimation();
            
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: cancellationToken);

            var enemyTransform = _enemyFacade.transform;
            _characterFacade.HealthSystem.GetDamage(_enemyConfiguration.Damage);

            Vector3 pushDirection = _characterFacade.transform.position - enemyTransform.position;
            pushDirection.y = 0f;
            pushDirection.Normalize();
            Vector3 force = pushDirection;
            _characterFacade.Rigidbody.AddForce(force * 10f, ForceMode.Impulse);
        }

        public async UniTask Tick(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var distanceToCharacter =
                    Vector3.Distance(_characterFacade.transform.position, _enemyFacade.transform.position);
                _cooldown -= Time.deltaTime;
                if (_cooldown <= 0 && distanceToCharacter <= _distanceExecuteDamage)
                {
                    await Execute(cancellationToken);
                    _cooldown = _enemyConfiguration.DamageCooldown;
                }

                await UniTask.Yield(cancellationToken);
            }
        }

        public void Initialize() =>
            _cooldown = _enemyConfiguration.DamageCooldown;
    }
}