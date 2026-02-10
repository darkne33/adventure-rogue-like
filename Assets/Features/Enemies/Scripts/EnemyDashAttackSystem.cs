using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    public class EnemyDashAttackSystem : IEnemyDamageSystem
    {
        private readonly CharacterFacade _characterFacade;
        private readonly EnemyConfiguration _enemyConfiguration;
        private readonly EnemyFacade _enemyFacade;

        private float _cooldown;
        private float _distanceExecuteDamage;
        private bool _canDamage;

        public EnemyDashAttackSystem(CharacterFacade characterFacade, EnemyConfiguration enemyConfiguration,
            EnemyFacade enemyFacade)
        {
            _characterFacade = characterFacade;
            _enemyConfiguration = enemyConfiguration;
            _enemyFacade = enemyFacade;
        }

        public void Initialize()
        {
            _distanceExecuteDamage = _enemyConfiguration.DamageRange;
            _cooldown = _enemyConfiguration.DamageCooldown;

            _enemyFacade.EnemyCollisionDetector.OnCollisionEnterEvent = ApplyDamage;
            _canDamage = true;
        }

        public async UniTask Execute(CancellationToken cancellationToken)
        {
            _canDamage = true;

            var enemyTransform = _enemyFacade.transform;

            _enemyFacade.SetStop(true);
            
            _enemyFacade.Rigidbody.linearVelocity = Vector3.zero;
            
             await enemyTransform.DOLookAt(_characterFacade.transform.position, 0.2f,
                axisConstraint: AxisConstraint.Y).ToUniTask(cancellationToken:  cancellationToken);
            
            await UniTask.Delay(
                TimeSpan.FromSeconds(1f),
                cancellationToken: cancellationToken
            );
            
            _enemyFacade.Rigidbody.AddForce(enemyTransform.forward * 20, ForceMode.Impulse);

            await UniTask.Delay(
                TimeSpan.FromSeconds(1f),
                cancellationToken: cancellationToken
            );

            _enemyFacade.Rigidbody.linearVelocity = Vector3.zero;
            _enemyFacade.SetStop(false);
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

        private void ApplyDamage()
        {
            _canDamage = false;
            _characterFacade.HealthSystem.GetDamage(_enemyConfiguration.Damage);
            _characterFacade.MoveSystem.CanMove(false);

            Vector3 pushDirection = _characterFacade.transform.position - _enemyFacade.transform.position;
            pushDirection.y = 0.5f;
            pushDirection.Normalize();
            float force = 20f;

            _characterFacade.Rigidbody.AddForce(pushDirection * force, ForceMode.Impulse);
        }
    }
}