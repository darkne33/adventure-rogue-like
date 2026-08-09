using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    public sealed class EnemyDamageAreaSystem : IEnemyDamageSystem
    {
        private readonly CharacterFacade _characterFacade;
        private readonly EnemyConfiguration _enemyConfiguration;
        private readonly EnemyFacade _enemyFacade;
        private readonly IEnemiesProvider _enemiesProvider;
        private readonly EnemyAreaDamageIndicatorView _indicatorView;
        private readonly float _attackPreparationDuration;
        private readonly List<EnemyFacade> _enemiesInDamageRadius = new();

        private float _cooldown;
        private bool _attackStarted;
        private bool _hasDetonated;

        public EnemyDamageAreaSystem(CharacterFacade characterFacade,
            EnemyConfiguration enemyConfiguration, EnemyFacade enemyFacade,
            IEnemiesProvider enemiesProvider, EnemyAreaDamageIndicatorView indicatorView,
            float attackPreparationDuration)
        {
            _characterFacade = characterFacade;
            _enemyConfiguration = enemyConfiguration;
            _enemyFacade = enemyFacade;
            _enemiesProvider = enemiesProvider;
            _indicatorView = indicatorView;
            _attackPreparationDuration = Mathf.Max(0f, attackPreparationDuration);
        }

        public void Initialize()
        {
            _cooldown = Mathf.Max(0f, _enemyConfiguration.InitialAttackCooldown);
            _indicatorView?.Initialize();

            if (_enemyConfiguration.ExplosionPrefab == null)
            {
                Debug.LogWarning(
                    $"{_enemyFacade.name} has no explosion prefab assigned. " +
                    "The Range Area attack will still deal damage.",
                    _enemyFacade);
            }
        }

        public async UniTask Execute(CancellationToken cancellationToken)
        {
            if (_attackStarted || _enemyFacade.IsDead || _enemyFacade.IsAggro == false)
                return;

            _attackStarted = true;
            Rigidbody rigidbody = _enemyFacade.Rigidbody;
            bool lockPosition =
                _enemyConfiguration.EnemyAnimationType == EnemyAnimationType.Bomb;
            Vector3 attackPosition = _enemyFacade.transform.position;

            try
            {
                _enemyFacade.SetStop(true);
                StopHorizontalMovement(rigidbody);
                if (lockPosition)
                    LockPosition(attackPosition, rigidbody);
                _indicatorView?.Show(
                    GetExplosionPosition(),
                    _enemyConfiguration.AreaDamageRadius,
                    _attackPreparationDuration);
                _enemyFacade.EffectsSystem.BeginAttackTelegraph(_attackPreparationDuration);

                _enemyFacade.AnimationSystem.IdleAnimation();
                _enemyFacade.AnimationSystem.AttackAnimation();

                float elapsed = 0f;
                while (elapsed < _attackPreparationDuration)
                {
                    if (_enemyFacade.IsDead)
                        return;

                    if (lockPosition)
                        LockPosition(attackPosition, rigidbody);

                    elapsed += Time.deltaTime;
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                if (_enemyFacade.IsDead || _enemyFacade.CanAttack == false)
                    return;

                if (lockPosition)
                    LockPosition(attackPosition, rigidbody);

                _indicatorView?.Complete(GetExplosionPosition());
                await _enemyFacade.EffectsSystem.CompleteAttackTelegraph(cancellationToken);

                if (_enemyFacade.IsDead || _enemyFacade.CanAttack == false)
                    return;

                if (lockPosition)
                    LockPosition(attackPosition, rigidbody);

                Detonate(killOwner: true);
            }
            finally
            {
                _indicatorView?.Hide();
                _enemyFacade?.EffectsSystem.ClearAttackTelegraph();

                if (_enemyFacade != null && _hasDetonated == false && _enemyFacade.IsDead == false)
                {
                    StopHorizontalMovement(rigidbody);
                    _enemyFacade.SyncNavigationPosition();
                    _enemyFacade.SetStop(false);
                    _attackStarted = false;
                }
            }
        }

        public async UniTask Tick(CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                _cooldown -= Time.deltaTime * _enemyFacade.RelicTimeScale;

                if (_attackStarted == false &&
                    _enemyFacade.IsDead == false &&
                    _enemyFacade.IsAggro &&
                    _enemyFacade.CanAttack &&
                    _enemyFacade.IsStopped == false &&
                    _cooldown <= 0f &&
                    IsCharacterInsideTriggerRange())
                {
                    await Execute(cancellationToken);

                    if (_hasDetonated)
                        return;

                    _cooldown = _enemyConfiguration.DamageCooldown;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        public void DetonateOnDeath()
        {
            if (_hasDetonated)
                return;

            _indicatorView?.Hide();
            _enemyFacade.EffectsSystem.ClearAttackTelegraph();
            Detonate(killOwner: false);
        }

        private void Detonate(bool killOwner)
        {
            if (_hasDetonated)
                return;

            _hasDetonated = true;
            Vector3 explosionPosition = GetExplosionPosition();

            SpawnExplosion(explosionPosition);

            if (IsCharacterInsideDamageRadius(explosionPosition))
            {
                _characterFacade.ReceiveDamage(
                    _enemyConfiguration.Damage, _enemyFacade);
            }

            DamageEnemiesInsideRadius(explosionPosition);

            if (killOwner == false)
                return;

            _enemyFacade.HealthSystem.GetDamage(int.MaxValue);

            if (_enemyFacade != null)
                _enemyFacade.gameObject.SetActive(false);
        }

        private Vector3 GetExplosionPosition() =>
            _enemyFacade.transform.position + _enemyConfiguration.ExplosionOffset;

        private void SpawnExplosion(Vector3 position)
        {
            GameObject explosionPrefab = _enemyConfiguration.ExplosionPrefab;
            if (explosionPrefab == null)
                return;

            GameObject explosion = UnityEngine.Object.Instantiate(
                explosionPrefab, position, Quaternion.identity);
            UnityEngine.Object.Destroy(
                explosion,
                Mathf.Max(0.1f, _enemyConfiguration.ExplosionEffectLifetime));
        }

        private bool IsCharacterInsideTriggerRange() =>
            IsCharacterInsideRadius(_enemyConfiguration.DamageRange);

        private bool IsCharacterInsideDamageRadius(Vector3 explosionPosition) =>
            IsCharacterInsideRadius(_enemyConfiguration.AreaDamageRadius, explosionPosition);

        private void DamageEnemiesInsideRadius(Vector3 explosionPosition)
        {
            if (_enemyConfiguration.DamagesEnemiesOnExplosion == false || _enemiesProvider == null)
                return;

            float radius = Mathf.Max(0f, _enemyConfiguration.AreaDamageRadius);
            float radiusSqr = radius * radius;
            IReadOnlyList<EnemyFacade> activeEnemies = _enemiesProvider.ActiveEnemies;
            _enemiesInDamageRadius.Clear();

            for (int index = 0; index < activeEnemies.Count; index++)
            {
                EnemyFacade enemy = activeEnemies[index];
                if (enemy == null || enemy == _enemyFacade ||
                    enemy.gameObject.activeInHierarchy == false || enemy.IsDead)
                    continue;

                Vector3 offset = enemy.transform.position - explosionPosition;
                offset.y = 0f;

                if (offset.sqrMagnitude <= radiusSqr)
                    _enemiesInDamageRadius.Add(enemy);
            }

            foreach (EnemyFacade enemy in _enemiesInDamageRadius)
            {
                if (enemy == null || enemy.IsDead)
                    continue;

                int appliedDamage = enemy.HealthSystem.GetDamage(_enemyConfiguration.Damage);
                if (appliedDamage > 0)
                    enemy.EffectsSystem.DealDamage();
            }
        }

        private bool IsCharacterInsideRadius(float radius)
        {
            return IsCharacterInsideRadius(radius, _enemyFacade.transform.position);
        }

        private bool IsCharacterInsideRadius(float radius, Vector3 center)
        {
            if (_characterFacade == null)
                return false;

            Vector3 offset = _characterFacade.transform.position - center;
            offset.y = 0f;
            float safeRadius = Mathf.Max(0f, radius);
            return offset.sqrMagnitude <= safeRadius * safeRadius;
        }

        private static void StopHorizontalMovement(Rigidbody rigidbody)
        {
            if (rigidbody == null)
                return;

            Vector3 velocity = rigidbody.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            rigidbody.linearVelocity = velocity;
        }

        private void LockPosition(Vector3 position, Rigidbody rigidbody)
        {
            if (rigidbody != null)
            {
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.position = position;
            }

            _enemyFacade.transform.position = position;
        }
    }
}
