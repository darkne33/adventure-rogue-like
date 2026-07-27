using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    public class EnemyDamageMeleeSystem : IEnemyDamageSystem
    {
        private const float TrackingDuration = 0.35f;
        private const float AttackHalfAngle = 65f;
        private const float KnockbackForce = 10f;

        private readonly CharacterFacade _characterFacade;
        private readonly EnemyConfiguration _enemyConfiguration;
        private readonly EnemyFacade _enemyFacade;
        private readonly float _attackPreparationDuration;

        private float _cooldown;
        private readonly float _distanceExecuteDamage;

        public EnemyDamageMeleeSystem(EnemyFacade enemyFacade, CharacterFacade characterFacade,
            EnemyConfiguration enemyConfiguration, float attackPreparationDuration)
        {
            _enemyFacade = enemyFacade;
            _characterFacade = characterFacade;
            _enemyConfiguration = enemyConfiguration;
            _attackPreparationDuration = Mathf.Max(0f, attackPreparationDuration);

            _distanceExecuteDamage = _enemyConfiguration.DamageRange;
        }

        public async UniTask Execute(CancellationToken cancellationToken)
        {
            if (_enemyFacade.IsDead || _enemyFacade.IsAggro == false)
                return;

            Rigidbody rigidbody = _enemyFacade.Rigidbody;
            _enemyFacade.SetStop(true);
            StopHorizontalMovement(rigidbody);
            _enemyFacade.EffectsSystem.BeginAttackTelegraph(_attackPreparationDuration);

            try
            {
                _enemyFacade.AnimationSystem.IdleAnimation();
                _enemyFacade.AnimationSystem.AttackAnimation();

                Transform enemyTransform = _enemyFacade.transform;
                float trackingDuration = Mathf.Min(
                    TrackingDuration, _attackPreparationDuration);
                await TrackCharacter(enemyTransform, trackingDuration, cancellationToken);

                Vector3 attackDirection = GetFlatDirection(enemyTransform.forward);
                await WaitForLockedWindup(
                    trackingDuration, _attackPreparationDuration, cancellationToken);

                if (_enemyFacade.IsDead)
                    return;

                await _enemyFacade.EffectsSystem.CompleteAttackTelegraph(cancellationToken);

                if (_enemyFacade.IsDead)
                    return;

                if (CanHitCharacter(enemyTransform, attackDirection) &&
                    _characterFacade.ReceiveDamage(_enemyConfiguration.Damage, _enemyFacade))
                {
                    Vector3 pushDirection = GetFlatDirection(
                        _characterFacade.transform.position - enemyTransform.position,
                        attackDirection);
                    _characterFacade.Rigidbody.AddForce(
                        pushDirection * KnockbackForce,
                        ForceMode.Impulse);
                }

                _enemyFacade.AnimationSystem.IdleAnimation();
                await UniTask.Delay(
                    TimeSpan.FromSeconds(Mathf.Max(
                        0f, _enemyConfiguration.MovementPauseAfterAttack)),
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
                _cooldown -= Time.deltaTime;

                if (_enemyFacade.IsDead == false &&
                    _enemyFacade.IsAggro &&
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

        public void Initialize() =>
            _cooldown = _enemyConfiguration.DamageCooldown;

        private async UniTask TrackCharacter(Transform enemyTransform, float duration,
            CancellationToken cancellationToken)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (_enemyFacade.IsDead)
                    return;

                Vector3 direction = GetFlatDirection(
                    _characterFacade.transform.position - enemyTransform.position,
                    enemyTransform.forward);
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                enemyTransform.rotation = Quaternion.RotateTowards(
                    enemyTransform.rotation,
                    targetRotation,
                    _enemyConfiguration.RotationSpeed * Time.deltaTime);

                elapsed += Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        private async UniTask WaitForLockedWindup(float elapsed, float duration,
            CancellationToken cancellationToken)
        {
            while (elapsed < duration)
            {
                if (_enemyFacade.IsDead)
                    return;

                elapsed += Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        private bool CanHitCharacter(Transform enemyTransform, Vector3 attackDirection)
        {
            Vector3 toCharacter = _characterFacade.transform.position - enemyTransform.position;
            toCharacter.y = 0f;

            float attackRange = Mathf.Max(0f, _distanceExecuteDamage);
            if (toCharacter.sqrMagnitude > attackRange * attackRange)
                return false;

            if (toCharacter.sqrMagnitude <= 0.001f)
                return true;

            float minimumAttackDot = Mathf.Cos(AttackHalfAngle * Mathf.Deg2Rad);
            return Vector3.Dot(attackDirection, toCharacter.normalized) >= minimumAttackDot;
        }

        private bool IsCharacterInsideAttackRange()
        {
            Vector3 toCharacter =
                _characterFacade.transform.position - _enemyFacade.transform.position;
            toCharacter.y = 0f;

            float attackRange = Mathf.Max(0f, _distanceExecuteDamage);
            return toCharacter.sqrMagnitude <= attackRange * attackRange;
        }

        private static Vector3 GetFlatDirection(Vector3 direction, Vector3 fallback = default)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
                return direction.normalized;

            fallback.y = 0f;
            return fallback.sqrMagnitude > 0.001f
                ? fallback.normalized
                : Vector3.forward;
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
