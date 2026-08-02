using System;
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
        private readonly float _attackPreparationDuration;

        private float _cooldown;
        private bool _attackStarted;
        private bool _hasDetonated;

        public EnemyDamageAreaSystem(CharacterFacade characterFacade,
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
            _cooldown = Mathf.Max(0f, _enemyConfiguration.InitialAttackCooldown);

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

            _enemyFacade.SetStop(true);
            StopHorizontalMovement(rigidbody);
            _enemyFacade.EffectsSystem.BeginAttackTelegraph(_attackPreparationDuration);

            try
            {
                _enemyFacade.AnimationSystem.IdleAnimation();
                _enemyFacade.AnimationSystem.AttackAnimation();

                await UniTask.Delay(
                    TimeSpan.FromSeconds(_attackPreparationDuration),
                    cancellationToken: cancellationToken);

                if (_enemyFacade.IsDead)
                    return;

                await _enemyFacade.EffectsSystem.CompleteAttackTelegraph(cancellationToken);

                if (_enemyFacade.IsDead)
                    return;

                _hasDetonated = true;
                Detonate();
            }
            finally
            {
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
                _cooldown -= Time.deltaTime;

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

        private void Detonate()
        {
            Vector3 explosionPosition =
                _enemyFacade.transform.position + _enemyConfiguration.ExplosionOffset;

            SpawnExplosion(explosionPosition);

            if (IsCharacterInsideDamageRadius(explosionPosition))
            {
                _characterFacade.ReceiveDamage(
                    _enemyConfiguration.Damage, _enemyFacade);
            }

            _enemyFacade.HealthSystem.GetDamage(int.MaxValue);

            if (_enemyFacade != null)
                _enemyFacade.gameObject.SetActive(false);
        }

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
    }
}
