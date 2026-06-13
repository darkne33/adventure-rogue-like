using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    public class EnemyDashAttackSystem : IEnemyDamageSystem
    {
        private const float WindupDuration = 0.65f;
        private const float DashDuration = 0.42f;
        private const float RecoveryDuration = 0.3f;
        private const float DashSpeed = 28f;
        private const float RotationSpeed = 720f;

        private readonly CharacterFacade _characterFacade;
        private readonly EnemyConfiguration _enemyConfiguration;
        private readonly EnemyFacade _enemyFacade;
        private readonly EnemyDashView _dashView;

        private float _cooldown;
        private float _distanceExecuteDamage;
        private bool _canDamage;

        public EnemyDashAttackSystem(CharacterFacade characterFacade, EnemyConfiguration enemyConfiguration,
            EnemyFacade enemyFacade, EnemyDashView dashView)
        {
            _characterFacade = characterFacade;
            _enemyConfiguration = enemyConfiguration;
            _enemyFacade = enemyFacade;
            _dashView = dashView;
        }

        public void Initialize()
        {
            _distanceExecuteDamage = _enemyConfiguration.DamageRange;
            _cooldown = _enemyConfiguration.DamageCooldown;
            _enemyFacade.EnemyCollisionDetector.OnCollisionEnterEvent = ApplyDamage;
            _canDamage = false;
        }

        public async UniTask Execute(CancellationToken cancellationToken)
        {
            Transform enemyTransform = _enemyFacade.transform;
            Rigidbody rigidbody = _enemyFacade.Rigidbody;
            Vector3 dashDirection = GetDirectionToCharacter(enemyTransform);

            _enemyFacade.SetStop(true);
            StopHorizontalMovement(rigidbody);

            try
            {
                float elapsed = 0f;
                while (elapsed < WindupDuration)
                {
                    dashDirection = GetDirectionToCharacter(enemyTransform);
                    RotateTowards(enemyTransform, dashDirection);

                    float progress = Mathf.Clamp01(elapsed / WindupDuration);
                    float distance = Vector3.Distance(enemyTransform.position, _characterFacade.transform.position);
                    float telegraphLength = Mathf.Max(_distanceExecuteDamage + 3f, distance + 2f);
                    _dashView?.ShowTelegraph(dashDirection, telegraphLength, progress);

                    elapsed += Time.deltaTime;
                    await UniTask.Yield(cancellationToken);
                }

                RotateTowards(enemyTransform, dashDirection, true);
                _dashView?.StartDash();
                _canDamage = true;

                rigidbody.linearVelocity = new Vector3(
                    dashDirection.x * DashSpeed,
                    rigidbody.linearVelocity.y,
                    dashDirection.z * DashSpeed);

                await UniTask.Delay(TimeSpan.FromSeconds(DashDuration), cancellationToken: cancellationToken);

                _canDamage = false;
                StopHorizontalMovement(rigidbody);
                _dashView?.StopDash();

                await UniTask.Delay(TimeSpan.FromSeconds(RecoveryDuration), cancellationToken: cancellationToken);
            }
            finally
            {
                _canDamage = false;
                _dashView?.StopDash();

                if (_enemyFacade != null)
                {
                    StopHorizontalMovement(rigidbody);
                    _enemyFacade.SyncNavigationPosition();
                    _enemyFacade.SetStop(false);
                }
            }
        }

        public async UniTask Tick(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                float distanceToCharacter =
                    Vector3.Distance(_characterFacade.transform.position, _enemyFacade.transform.position);
                _cooldown -= Time.deltaTime;

                if (_enemyFacade.IsStopped == false &&
                    _cooldown <= 0f &&
                    distanceToCharacter <= _distanceExecuteDamage)
                {
                    await Execute(cancellationToken);
                    _cooldown = _enemyConfiguration.DamageCooldown;
                }

                await UniTask.Yield(cancellationToken);
            }
        }

        private void ApplyDamage()
        {
            if (_canDamage == false)
                return;

            _canDamage = false;
            bool damageApplied = _characterFacade.ReceiveDamage(_enemyConfiguration.Damage, _enemyFacade);

            if (damageApplied == false)
                return;

            _characterFacade.MoveSystem.CanMove(false);

            Vector3 pushDirection = _characterFacade.transform.position - _enemyFacade.transform.position;
            pushDirection.y = 0.5f;
            pushDirection.Normalize();
            _characterFacade.Rigidbody.AddForce(pushDirection * 20f, ForceMode.Impulse);
        }

        private Vector3 GetDirectionToCharacter(Transform enemyTransform)
        {
            Vector3 direction = _characterFacade.transform.position - enemyTransform.position;
            direction.y = 0f;

            return direction.sqrMagnitude > 0.001f
                ? direction.normalized
                : enemyTransform.forward;
        }

        private static void StopHorizontalMovement(Rigidbody rigidbody)
        {
            Vector3 velocity = rigidbody.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            rigidbody.linearVelocity = velocity;
        }

        private static void RotateTowards(Transform enemyTransform, Vector3 direction, bool immediately = false)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            enemyTransform.rotation = immediately
                ? targetRotation
                : Quaternion.RotateTowards(enemyTransform.rotation, targetRotation, RotationSpeed * Time.deltaTime);
        }
    }
}
