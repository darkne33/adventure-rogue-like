using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    public sealed class EnemyBulletAttackSystem : IEnemyDamageSystem
    {
        private const float RotationSpeed = 720f;

        private readonly CharacterFacade _characterFacade;
        private readonly EnemyConfiguration _enemyConfiguration;
        private readonly EnemyFacade _enemyFacade;
        private readonly float _attackPreparationDuration;

        private EnemyBulletConfiguration _bulletConfiguration;
        private float _cooldown;
        private float _attackDistance;

        public EnemyBulletAttackSystem(CharacterFacade characterFacade,
            EnemyConfiguration enemyConfiguration, EnemyFacade enemyFacade,
            float attackPreparationDuration)
        {
            _characterFacade = characterFacade;
            _enemyConfiguration = enemyConfiguration;
            _enemyFacade = enemyFacade;
            _attackPreparationDuration = Mathf.Max(0f, attackPreparationDuration);
        }

        public void Initialize()
        {
            _bulletConfiguration = _enemyConfiguration.BulletConfiguration;

            if (_bulletConfiguration == null)
                throw new InvalidOperationException(
                    $"{_enemyFacade.name} requires an EnemyBulletConfiguration for RangeBullet attacks.");

            if (_bulletConfiguration.ProjectilePrefab == null)
                throw new InvalidOperationException(
                    $"{_bulletConfiguration.name} requires a projectile prefab.");

            _attackDistance = _enemyConfiguration.DamageRange;
            _cooldown = Mathf.Max(0f, _enemyConfiguration.InitialAttackCooldown);
        }

        public async UniTask Execute(CancellationToken cancellationToken)
        {
            if (_enemyFacade.IsDead || _enemyFacade.IsAggro == false)
                return;

            Transform enemyTransform = _enemyFacade.transform;
            Rigidbody rigidbody = _enemyFacade.Rigidbody;
            bool projectileReleased = false;

            _enemyFacade.SetStop(true);
            StopHorizontalMovement(rigidbody);
            _enemyFacade.EffectsSystem.BeginAttackTelegraph(_attackPreparationDuration);

            try
            {
                _enemyFacade.AnimationSystem.IdleAnimation();
                _enemyFacade.AnimationSystem.AttackAnimation();

                float elapsed = 0f;
                while (elapsed < _attackPreparationDuration)
                {
                    if (_enemyFacade.IsDead)
                        return;

                    RotateTowardsCharacter(enemyTransform);
                    elapsed += Time.deltaTime;
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                if (_enemyFacade.IsDead)
                    return;

                await _enemyFacade.EffectsSystem.CompleteAttackTelegraph(cancellationToken);

                if (_enemyFacade.IsDead || _enemyFacade.CanAttack == false)
                    return;

                RotateTowardsCharacter(enemyTransform, true);
                SpawnProjectile(cancellationToken);
                projectileReleased = true;

                _enemyFacade.AnimationSystem.IdleAnimation();
                float movementPause = Mathf.Max(
                    _bulletConfiguration.RecoveryDuration,
                    _enemyConfiguration.MovementPauseAfterAttack);
                await UniTask.Delay(TimeSpan.FromSeconds(movementPause),
                    cancellationToken: cancellationToken);
            }
            finally
            {
                _enemyFacade?.EffectsSystem.ClearAttackTelegraph();

                if (_enemyFacade != null)
                {
                    StopHorizontalMovement(rigidbody);
                    _enemyFacade.SyncNavigationPosition();
                    _enemyFacade.SetStop(false);

                    if (projectileReleased && _enemyFacade.IsDead == false)
                        _enemyFacade.NotifyAttackFinished();
                }
            }
        }

        public async UniTask Tick(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _cooldown -= Time.deltaTime * _enemyFacade.RelicTimeScale;

                if (_enemyFacade.IsDead == false &&
                    _enemyFacade.IsAggro &&
                    _enemyFacade.CanAttack &&
                    _enemyFacade.IsStopped == false &&
                    _cooldown <= 0f &&
                    IsCharacterInsideAttackRange())
                {
                    await Execute(cancellationToken);
                    _cooldown = _enemyConfiguration.DamageCooldown;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        private bool IsCharacterInsideAttackRange()
        {
            float distanceToCharacter = GetFlatDistanceToCharacter();
            return distanceToCharacter <= _attackDistance;
        }

        private float GetFlatDistanceToCharacter()
        {
            Vector3 offset = _characterFacade.transform.position - _enemyFacade.transform.position;
            offset.y = 0f;
            return offset.magnitude;
        }

        private void SpawnProjectile(CancellationToken cancellationToken)
        {
            Vector3 startPosition = _enemyFacade.TargetToShootDamage != null
                ? _enemyFacade.TargetToShootDamage.position
                : _enemyFacade.transform.position + Vector3.up;
            Vector3 targetPosition = _characterFacade.ProjectileSpawnPosition +
                                     _bulletConfiguration.TargetOffset;
            Vector3 direction = targetPosition - startPosition;

            if (direction.sqrMagnitude <= 0.0001f)
                direction = _enemyFacade.transform.forward;

            direction.Normalize();
            Quaternion rotation = Quaternion.LookRotation(direction);
            EnemyBullet projectile = UnityEngine.Object.Instantiate(
                _bulletConfiguration.ProjectilePrefab, startPosition, rotation);
            float relicTimeScale = Mathf.Max(0.05f, _enemyFacade.RelicTimeScale);
            projectile.Launch(startPosition, direction, _bulletConfiguration.Speed * relicTimeScale,
                _bulletConfiguration.Lifetime / relicTimeScale, _enemyConfiguration.Damage, _enemyFacade,
                _characterFacade, cancellationToken);
        }

        private void RotateTowardsCharacter(Transform enemyTransform, bool immediately = false)
        {
            Vector3 direction = _characterFacade.transform.position - enemyTransform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            enemyTransform.rotation = immediately
                ? targetRotation
                : Quaternion.RotateTowards(enemyTransform.rotation, targetRotation,
                    RotationSpeed * Time.deltaTime);
        }

        private static void StopHorizontalMovement(Rigidbody rigidbody)
        {
            Vector3 velocity = rigidbody.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            rigidbody.linearVelocity = velocity;
        }
    }
}
