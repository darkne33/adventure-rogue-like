using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Features.Bosses.Scripts
{
    public sealed class GolemBossAttackSystem : BossAttackSystem
    {
        private readonly GolemBossFacade _boss;
        private readonly CharacterFacade _character;
        private readonly List<GameObject> _impactEffects = new();
        private GolemBossConfiguration _configuration;
        private GolemBossAnimation _animation;
        private BossRadialAttack _radialAttack;
        private float _wait;
        private int _lastClockFrame = -1;
        private bool _executing;

        public GolemBossAttackSystem(GolemBossFacade boss, CharacterFacade character) : base(boss, character)
        {
            _boss = boss;
            _character = character;
        }

        public override void Initialize()
        {
            _configuration = _boss.MovementConfiguration;
            _animation = _boss.AnimationSystem as GolemBossAnimation;
            _radialAttack = _boss.RangeRadiusAttack != null
                ? new BossRadialAttack(_boss.RangeRadiusAttack, _boss, _character)
                : null;
            _wait = Mathf.Max(0f, _boss.Config.InitialAttackDelay);
            _lastClockFrame = -1;
            _executing = false;
            _animation?.IdleAnimation();
        }

        public override async UniTask Tick(CancellationToken cancellationToken)
        {
            try
            {
                await RunCycle(cancellationToken);
            }
            finally
            {
                foreach (GameObject effect in _impactEffects)
                    DestroyVisual(effect);
                _impactEffects.Clear();
            }
        }

        private async UniTask RunCycle(CancellationToken cancellationToken)
        {
            while (_boss != null && !_boss.IsDead)
            {
                cancellationToken.ThrowIfCancellationRequested();
                float delta = AdvanceClock();
                if (CanContinue() && _boss.CanAttack && delta > 0f && _configuration != null && _animation != null)
                {
                    _wait = Mathf.Max(0f, _wait - delta);
                    if (_wait <= 0f)
                    {
                        if (_radialAttack != null && _radialAttack.IsReady)
                            await ExecuteRadialAttack(cancellationToken);
                        else
                        {
                            GolemSlamAttackConfiguration attack = FindAttack();
                            FaceCharacter();
                            if (attack != null && TryGetImpact(attack, out _))
                                await ExecuteAttack(attack, cancellationToken);
                            else if (attack != null)
                                MoveTowardsCharacter(delta);
                            else
                                _animation.IdleAnimation();
                        }
                    }
                    else
                        _animation.IdleAnimation();
                }
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        public override async UniTask Execute(CancellationToken cancellationToken)
        {
            if (_radialAttack != null && _radialAttack.IsReady)
                await ExecuteRadialAttack(cancellationToken);
            else
                await ExecuteAttack(FindAttack(), cancellationToken);
        }

        private async UniTask ExecuteRadialAttack(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!CanStartAttack() || _radialAttack == null || !_radialAttack.IsReady)
                return;
            _executing = true;
            _boss.CombatSystem.SetAttacking(true);
            try
            {
                StopVelocity();
                await _radialAttack.Execute(cancellationToken);
            }
            finally
            {
                _wait = Mathf.Max(0f, _radialAttack.Configuration.RecoveryDuration);
                FinishAttack(true);
            }
        }

        internal async UniTask ExecuteAttack(GolemSlamAttackConfiguration attack,
            CancellationToken cancellationToken, bool animateBoss = true, Func<bool> ownsAnimation = null)
        {
            bool CanAnimate() => animateBoss && (ownsAnimation?.Invoke() ?? true);

            cancellationToken.ThrowIfCancellationRequested();
            if (!CanStartAttack() || attack == null || !attack.IsEnabled)
                return;
            FaceCharacter();
            if (!TryGetImpact(attack, out Vector3 impactPoint))
                return;

            // Capture the ground directly below the root. The animation cannot redirect this circle.
            Vector3 direction = _boss.AttackRotation * Vector3.forward;
            float duration = Mathf.Max(0.01f, attack.Duration);
            float impact = Mathf.Clamp(attack.ImpactNormalized, 0.01f, 1f);
            float elapsed = 0f;
            bool hasHit = false;
            GameObject indicator = null;
            GameObject effect = null;
            Vector3 indicatorScale = Vector3.one;
            _executing = true;
            _boss.CombatSystem.SetAttacking(true);
            StopVelocity();
            try
            {
                if (attack.IndicatorPrefab != null)
                {
                    indicator = Object.Instantiate(attack.IndicatorPrefab,
                        impactPoint + Vector3.up * attack.IndicatorGroundOffset, Quaternion.identity);
                    foreach (Collider collider in indicator.GetComponentsInChildren<Collider>(true))
                        collider.enabled = false;
                    indicatorScale = indicator.transform.localScale;
                    UpdateWarning(indicator.transform, indicatorScale, attack, 0f);
                    indicator.SetActive(true);
                }
                if (CanAnimate())
                    _animation.BeginTimedAttack(duration);

                while (elapsed < duration)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!CanContinue())
                        return;
                    float delta = AdvanceClock();
                    if (delta <= 0f)
                        continue;
                    elapsed = Mathf.Min(duration, elapsed + delta);
                    float progress = elapsed / duration;
                    if (!hasHit && indicator != null)
                        UpdateWarning(indicator.transform, indicatorScale, attack, progress / impact);
                    if (!hasHit && progress >= impact)
                    {
                        hasHit = true;
                        DestroyVisual(indicator);
                        indicator = null;
                        if (attack.EffectPrefab != null)
                        {
                            effect = Object.Instantiate(attack.EffectPrefab, impactPoint, Quaternion.identity);
                            _impactEffects.RemoveAll(active => active == null);
                            _impactEffects.Add(effect);
                            Object.Destroy(effect, Mathf.Max(0.1f, attack.EffectLifetime));
                        }
                        _boss.PlayCombatSound(Features.Sounds.SoundId.BossSlam);
                        ApplyHit(impactPoint, attack, direction);
                    }
                }
            }
            finally
            {
                DestroyVisual(indicator);
                if (cancellationToken.IsCancellationRequested || !CanContinue())
                    DestroyVisual(effect);
                _wait = Mathf.Max(0f, attack.RecoveryDuration);
                FinishAttack(CanAnimate());
            }
        }

        private void FinishAttack(bool animateBoss)
        {
            _executing = false;
            if (_boss == null)
                return;
            StopVelocity();
            if (!_boss.IsDead && _boss.isActiveAndEnabled && animateBoss)
                _animation?.IdleAnimation();
            _boss.CombatSystem.SetAttacking(false);
        }

        private GolemSlamAttackConfiguration FindAttack()
        {
            HealthSystem health = _boss.HealthSystem;
            float percentage = health.CurrentHealth / Mathf.Max(1f, health.MaxHealth) * 100f;
            BossAttackConfiguration[] attacks = _boss.Config.GetAttacksForHealth(percentage);
            if (attacks != null)
                foreach (BossAttackConfiguration attack in attacks)
                    if (attack is GolemSlamAttackConfiguration { IsEnabled: true } slam)
                        return slam;
            return null;
        }

        private bool CanContinue() => _boss != null && !_boss.IsDead && _boss.isActiveAndEnabled &&
            _character != null && _character.isActiveAndEnabled && _character.HealthSystem != null &&
            !_character.HealthSystem.IsDead && !_character.IsTransitionPaused;

        private bool CanStartAttack() => !_executing && _configuration != null && _animation != null &&
            CanContinue() && !_character.IsCinematicPaused && _boss.CanAttack && Time.deltaTime > 0f;

        private float AdvanceClock()
        {
            float scale = CanContinue() && !_character.IsCinematicPaused ? _boss.RelicTimeScale : 0f;
            _animation?.SetTimeScale(scale);
            if (_lastClockFrame == Time.frameCount)
                return 0f;
            _lastClockFrame = Time.frameCount;
            return Time.deltaTime * scale;
        }

        private void FaceCharacter()
        {
            Vector3 direction = _character.transform.position - _boss.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.001f)
                return;
            Quaternion rotation = BossFacade.GetFlatRotation(direction);
            if (_boss.Rigidbody != null)
                _boss.Rigidbody.rotation = rotation;
            _boss.transform.rotation = rotation;
            StopVelocity();
        }

        private bool TryGetImpact(GolemSlamAttackConfiguration attack, out Vector3 impactPoint)
        {
            // Never use animated hand, weapon or body transforms to position the damage footprint.
            if (!TryGetGround(_boss.transform.position, out impactPoint))
                return false;
            Vector3 target = _character.transform.position - impactPoint;
            target.y = 0f;
            float radius = Mathf.Max(0.01f, attack.Radius);
            return target.sqrMagnitude <= radius * radius;
        }

        private void MoveTowardsCharacter(float delta)
        {
            Vector3 direction = _character.transform.position - _boss.transform.position;
            direction.y = 0f;
            float distance = Mathf.Min(direction.magnitude, Mathf.Max(0f, _configuration.MoveSpeed) * delta);
            if (distance <= 0f || !TryGetGround(_boss.transform.position, out Vector3 startGround))
            {
                _animation.IdleAnimation();
                return;
            }
            direction.Normalize();
            float radius = Mathf.Max(0.05f, _configuration.BodyRadius);
            if (_boss.Collider != null)
                radius = Mathf.Max(radius,
                    Mathf.Max(_boss.Collider.bounds.extents.x, _boss.Collider.bounds.extents.z));
            foreach (RaycastHit hit in Physics.SphereCastAll(startGround + Vector3.up * (radius + 0.05f),
                         radius, direction, distance, _configuration.MovementObstacleMask,
                         QueryTriggerInteraction.Ignore))
                if (hit.collider != null && !hit.collider.transform.IsChildOf(_boss.transform))
                    distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - 0.05f));

            Vector3 candidate = startGround + direction * distance;
            Vector3 side = Vector3.Cross(Vector3.up, direction) * radius * 0.7f;
            if (distance <= 0f || !TryGetGround(candidate, out Vector3 ground) ||
                Mathf.Abs(ground.y - startGround.y) > radius ||
                !HasFloor(ground + side, ground.y, radius) || !HasFloor(ground - side, ground.y, radius) ||
                !HasFloor(ground + direction * radius * 0.7f, ground.y, radius))
            {
                _animation.IdleAnimation();
                return;
            }
            Vector3 position = _boss.transform.position + ground - startGround;
            if (_boss.Rigidbody != null)
                _boss.Rigidbody.position = position;
            _boss.transform.position = position;
            _animation.MoveAnimation();
        }

        private bool HasFloor(Vector3 position, float height, float tolerance) =>
            TryGetGround(position, out Vector3 ground) && Mathf.Abs(ground.y - height) <= tolerance;

        private bool TryGetGround(Vector3 position, out Vector3 ground)
        {
            float height = Mathf.Max(0.01f, _configuration.GroundProbeHeight);
            if (Physics.Raycast(position + Vector3.up * height, Vector3.down, out RaycastHit hit,
                    height + Mathf.Max(0.01f, _configuration.GroundProbeDistance), _configuration.GroundMask,
                    QueryTriggerInteraction.Ignore) && hit.normal.y >= 0.5f &&
                !hit.collider.transform.IsChildOf(_boss.transform))
            {
                ground = hit.point;
                return true;
            }
            ground = position;
            return false;
        }

        private void StopVelocity()
        {
            Rigidbody body = _boss.Rigidbody;
            if (body == null || body.isKinematic)
                return;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        private static void UpdateWarning(Transform indicator, Vector3 originalScale,
            GolemSlamAttackConfiguration attack, float progress)
        {
            float growth = Mathf.Lerp(Mathf.Clamp(attack.InitialIndicatorScale, 0.01f, 1f),
                1f, Mathf.Clamp01(progress));
            float diameter = Mathf.Max(0.01f, attack.Radius) * 2f * growth;
            indicator.localScale = new Vector3(originalScale.x * diameter, originalScale.y,
                originalScale.z * diameter);
        }

        private void ApplyHit(Vector3 ground, GolemSlamAttackConfiguration attack, Vector3 fallbackDirection)
        {
            // Keep the Mushroom jump shockwave's grounded-character dodge rule.
            if (_character.MoveSystem?.IsGrounded == false)
                return;
            float radius = Mathf.Max(0.01f, attack.Radius);
            foreach (Collider hit in Physics.OverlapCapsule(ground, ground + Vector3.up * radius, radius,
                         Physics.AllLayers, QueryTriggerInteraction.Collide))
            {
                if (hit.GetComponentInParent<CharacterFacade>() != _character)
                    continue;
                if (_character.ReceiveAreaDamage(attack.Damage, _boss, ground,
                        ground + Vector3.up * radius, radius) && _character.Rigidbody != null &&
                    !_character.Rigidbody.isKinematic)
                {
                    Vector3 direction = _character.transform.position - ground;
                    direction.y = 0f;
                    direction = direction.sqrMagnitude > 0.001f ? direction.normalized : fallbackDirection;
                    _character.Rigidbody.AddForce(direction * Mathf.Max(0f, attack.KnockbackForce) +
                        Vector3.up * Mathf.Max(0f, attack.KnockbackUpwardForce), ForceMode.Impulse);
                }
                // Multiple character colliders must not multiply one impact's damage.
                return;
            }
        }

        private static void DestroyVisual(GameObject visual)
        {
            if (visual == null)
                return;
            visual.SetActive(false);
            Object.Destroy(visual);
        }
    }
}
