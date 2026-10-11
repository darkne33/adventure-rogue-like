using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Features.Bosses.Scripts
{
    public sealed class OgreBossAttackSystem : BossAttackSystem
    {
        private readonly OgreBossFacade _boss;
        private readonly CharacterFacade _character;
        private OgreBossConfiguration _configuration;
        private OgreBossAnimation _animation;
        private float _cooldown;
        private float _wait;
        private int _lastClockFrame = -1;
        private bool _executing;

        public OgreBossAttackSystem(OgreBossFacade boss, CharacterFacade character) : base(boss, character)
        {
            _boss = boss;
            _character = character;
        }

        public override void Initialize()
        {
            _configuration = _boss.MovementConfiguration;
            _animation = _boss.AnimationSystem as OgreBossAnimation;
            _cooldown = 0f;
            _wait = Mathf.Max(0f, _boss.Config.InitialAttackDelay);
            _lastClockFrame = -1;
            _executing = false;
            _animation?.IdleAnimation();
        }

        public override async UniTask Tick(CancellationToken cancellationToken)
        {
            while (_boss != null && !_boss.IsDead)
            {
                cancellationToken.ThrowIfCancellationRequested();
                float delta = AdvanceClock();
                if (CanContinue() && _boss.CanAttack && delta > 0f && _configuration != null && _animation != null)
                {
                    _wait = Mathf.Max(0f, _wait - delta);
                    OgreSlamAttackConfiguration attack = FindAttack();
                    if (_wait <= 0f && attack != null)
                    {
                        FaceCharacter();
                        if (TryGetImpact(attack, out _))
                        {
                            _animation.IdleAnimation();
                            if (_cooldown <= 0f)
                                await ExecuteAttack(attack, cancellationToken);
                        }
                        else
                            MoveTowardsCharacter(delta);
                    }
                    else
                        _animation.IdleAnimation();
                }
                else if (!_executing)
                    _animation?.IdleAnimation();
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        public override UniTask Execute(CancellationToken cancellationToken) =>
            ExecuteAttack(FindAttack(), cancellationToken);

        internal async UniTask ExecuteAttack(OgreSlamAttackConfiguration attack,
            CancellationToken cancellationToken, bool animateBoss = true, Func<bool> ownsAnimation = null)
        {
            bool CanAnimate() => animateBoss && (ownsAnimation?.Invoke() ?? true);

            cancellationToken.ThrowIfCancellationRequested();
            if (_executing || attack == null || !attack.IsEnabled || _configuration == null || _animation == null ||
                !CanContinue() || !_boss.CanAttack || _cooldown > 0f || Time.deltaTime <= 0f)
                return;
            FaceCharacter();
            if (!TryGetImpact(attack, out Vector3 impactPoint))
                return;

            // Capture before awaiting: dodging cannot redirect the warning or the impact.
            Vector3 direction = _boss.AttackRotation * Vector3.forward;
            Vector3 effectPoint = impactPoint;
            effectPoint.y = _boss.ImpactOrigin.position.y;
            float duration = Mathf.Max(0.01f, attack.Duration);
            float impact = Mathf.Clamp(attack.ImpactNormalized, 0.01f, 1f);
            float elapsed = 0f;
            bool hasHit = false;
            GameObject indicator = null;
            Vector3 indicatorScale = Vector3.one;
            _executing = true;
            _cooldown = Mathf.Max(0f, attack.Cooldown);
            _boss.CombatSystem.SetAttacking(true);

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
                        DestroyIndicator(indicator);
                        indicator = null;
                        if (attack.EffectPrefab != null)
                        {
                            GameObject effect = Object.Instantiate(attack.EffectPrefab, effectPoint, Quaternion.identity);
                            Object.Destroy(effect, Mathf.Max(0.1f, attack.EffectLifetime));
                        }
                        ApplyHit(impactPoint, attack, direction);
                    }
                }
            }
            finally
            {
                DestroyIndicator(indicator);
                _executing = false;
                _wait = Mathf.Max(0f, attack.RecoveryDuration);
                if (_boss != null)
                {
                    if (!_boss.IsDead && _boss.isActiveAndEnabled && CanAnimate())
                        _animation.IdleAnimation();
                    _boss.CombatSystem.SetAttacking(false);
                }
            }
        }

        private OgreSlamAttackConfiguration FindAttack()
        {
            HealthSystem health = _boss.HealthSystem;
            float percentage = health.CurrentHealth / Mathf.Max(1f, health.MaxHealth) * 100f;
            BossAttackConfiguration[] attacks = _boss.Config.GetAttacksForHealth(percentage);
            if (attacks != null)
                foreach (BossAttackConfiguration candidate in attacks)
                    if (candidate is OgreSlamAttackConfiguration { IsEnabled: true } attack)
                        return attack;
            return null;
        }

        private bool CanContinue() => _boss != null && !_boss.IsDead && _boss.isActiveAndEnabled &&
            _character != null && _character.HealthSystem != null && !_character.HealthSystem.IsDead &&
            !_character.IsTransitionPaused;

        private float AdvanceClock()
        {
            float scale = CanContinue() ? _boss.RelicTimeScale : 0f;
            _animation?.SetTimeScale(scale);
            if (_lastClockFrame == Time.frameCount)
                return 0f;
            _lastClockFrame = Time.frameCount;
            float delta = Time.deltaTime * scale;
            _cooldown = Mathf.Max(0f, _cooldown - delta);
            return delta;
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
        }

        private bool TryGetImpact(OgreSlamAttackConfiguration attack, out Vector3 impactPoint)
        {
            impactPoint = default;
            Vector3 target = _character.transform.position - _boss.transform.position;
            target.y = 0f;
            if (target.sqrMagnitude > Mathf.Max(0f, attack.Range) * Mathf.Max(0f, attack.Range) ||
                !TryGetGround(_boss.ImpactOrigin.position, out impactPoint))
                return false;
            target = _character.transform.position - impactPoint;
            target.y = 0f;
            // Approach until the player is inside the actual warning, rather than using range alone.
            return target.sqrMagnitude <= Mathf.Max(0.01f, attack.Radius) * Mathf.Max(0.01f, attack.Radius);
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

        private static void UpdateWarning(Transform indicator, Vector3 originalScale,
            OgreSlamAttackConfiguration attack, float progress)
        {
            float growth = Mathf.Lerp(Mathf.Clamp(attack.InitialIndicatorScale, 0.01f, 1f),
                1f, Mathf.Clamp01(progress));
            float diameter = Mathf.Max(0.01f, attack.Radius) * 2f * growth;
            indicator.localScale = new Vector3(originalScale.x * diameter, originalScale.y,
                originalScale.z * diameter);
        }

        private void ApplyHit(Vector3 ground, OgreSlamAttackConfiguration attack, Vector3 fallbackDirection)
        {
            float radius = Mathf.Max(0.01f, attack.Radius);
            foreach (Collider hit in Physics.OverlapCapsule(ground, ground + Vector3.up * radius, radius,
                         Physics.AllLayers, QueryTriggerInteraction.Collide))
            {
                if (hit.GetComponentInParent<CharacterFacade>() != _character)
                    continue;
                if (_character.ReceiveDamage(attack.Damage, _boss) && _character.Rigidbody != null &&
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

        private static void DestroyIndicator(GameObject indicator)
        {
            if (indicator == null)
                return;
            indicator.SetActive(false);
            Object.Destroy(indicator);
        }
    }
}
