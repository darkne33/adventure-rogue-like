using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    public sealed class EnemyRangedAttackSystem : IEnemyDamageSystem
    {
        private const float RotationSpeed = 720f;

        private readonly CharacterFacade _characterFacade;
        private readonly EnemyConfiguration _enemyConfiguration;
        private readonly EnemyFacade _enemyFacade;
        private readonly EnemyRangedAttackView _attackView;
        private readonly float _attackPreparationDuration;

        private float _cooldown;
        private float _minimumDistanceExecuteDamage;
        private float _distanceExecuteDamage;

        public EnemyRangedAttackSystem(CharacterFacade characterFacade, EnemyConfiguration enemyConfiguration,
            EnemyFacade enemyFacade, EnemyRangedAttackView attackView,
            float attackPreparationDuration)
        {
            _characterFacade = characterFacade;
            _enemyConfiguration = enemyConfiguration;
            _enemyFacade = enemyFacade;
            _attackView = attackView;
            _attackPreparationDuration = Mathf.Max(0f, attackPreparationDuration);
        }

        public void Initialize()
        {
            if (_attackView == null)
                throw new InvalidOperationException($"{_enemyFacade.name} requires EnemyRangedAttackView.");

            if (_attackView.ProjectilePrefab == null)
                throw new InvalidOperationException($"{_enemyFacade.name} requires a projectile prefab.");

            _minimumDistanceExecuteDamage = _attackView.MinimumAttackDistance;
            _distanceExecuteDamage = _enemyConfiguration.DamageRange;
            _cooldown = _enemyConfiguration.DamageCooldown;
        }

        public async UniTask Execute(CancellationToken cancellationToken)
        {
            if (_enemyFacade.IsDead || _enemyFacade.IsAggro == false)
                return;

            Transform enemyTransform = _enemyFacade.transform;
            Rigidbody rigidbody = _enemyFacade.Rigidbody;

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

                await _enemyFacade.EffectsSystem.CompleteAttackTelegraph(cancellationToken);

                if (_enemyFacade.IsDead || _enemyFacade.CanAttack == false)
                    return;

                RotateTowardsCharacter(enemyTransform, true);
                SpawnProjectile(cancellationToken);

                _enemyFacade.AnimationSystem.IdleAnimation();
                float movementPause = Mathf.Max(
                    _attackView.RecoveryDuration,
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
                }
            }
        }

        public async UniTask Tick(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                float distanceToCharacter = GetFlatDistanceToCharacter();
                _cooldown -= Time.deltaTime * _enemyFacade.RelicTimeScale;

                if (_enemyFacade.IsDead == false &&
                    _enemyFacade.IsAggro &&
                    _enemyFacade.IsStopped == false &&
                    _cooldown <= 0f &&
                    distanceToCharacter >= _minimumDistanceExecuteDamage &&
                    distanceToCharacter <= _distanceExecuteDamage)
                {
                    await Execute(cancellationToken);
                    _cooldown = _enemyConfiguration.DamageCooldown;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        private float GetFlatDistanceToCharacter()
        {
            Vector3 offset = _characterFacade.transform.position - _enemyFacade.transform.position;
            offset.y = 0f;
            return offset.magnitude;
        }

        private void SpawnProjectile(CancellationToken cancellationToken)
        {
            Transform spawnPoint = _attackView.ProjectileSpawnPoint;
            Vector3 startPosition = spawnPoint.position;
            Vector3 targetPosition = _characterFacade.transform.position + _attackView.TargetOffset;
            Vector3 direction = targetPosition - startPosition;
            Quaternion rotation = direction.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(direction.normalized)
                : Quaternion.identity;

            EnemyCannonball projectile = UnityEngine.Object.Instantiate(
                _attackView.ProjectilePrefab, startPosition, rotation);
            projectile.Launch(startPosition, targetPosition,
                _attackView.ProjectileFlightDuration / Mathf.Max(0.05f, _enemyFacade.RelicTimeScale),
                _attackView.ProjectileArcHeight, _attackView.ImpactRadius, _enemyConfiguration.Damage,
                _enemyFacade, _characterFacade, cancellationToken);
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
                : Quaternion.RotateTowards(enemyTransform.rotation, targetRotation, RotationSpeed * Time.deltaTime);
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
