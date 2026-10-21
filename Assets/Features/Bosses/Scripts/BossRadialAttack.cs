using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Features.Bosses.Scripts
{
    public sealed class BossRadialAttack : IBossAttack
    {
        public BossRadialAttackConfiguration Configuration { get; }
        public bool IsReady => !_executing && Configuration.IsEnabled && Configuration.ProjectilePrefab != null &&
                               _boss != null && _boss.RadialAttackClock >= _readyAt;

        private readonly BossFacade _boss;
        private readonly CharacterFacade _character;
        private float _readyAt;
        private bool _executing;

        public BossRadialAttack(BossRadialAttackConfiguration configuration, BossFacade boss,
            CharacterFacade character)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _boss = boss;
            _character = character;
        }

        public async UniTask Execute(CancellationToken cancellationToken, bool animateBoss = true,
            Func<bool> ownsAnimation = null)
        {
            bool CanAnimate() => animateBoss && (ownsAnimation?.Invoke() ?? true);

            cancellationToken.ThrowIfCancellationRequested();
            if (!IsReady || !CanContinue() || !_boss.CanAttack || Time.deltaTime <= 0f)
                return;

            _executing = true;
            _readyAt = _boss.RadialAttackClock + Mathf.Max(0.01f, Configuration.Cooldown);
            FaceCharacter();
            try
            {
                float warning = Mathf.Max(0.01f, Configuration.WarningDuration);
                if (CanAnimate())
                {
                    _boss.AnimationSystem.IdleAnimation();
                    _boss.EffectsSystem.BeginAttackTelegraph(warning);
                    _boss.EffectsSystem.SetAttackTelegraphTimeScale(CurrentTimeScale());
                }

                float elapsed = 0f;
                while (elapsed < warning)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!CanContinue())
                        return;
                    if (CanAnimate())
                        _boss.EffectsSystem.SetAttackTelegraphTimeScale(CurrentTimeScale());
                    else
                        _boss.EffectsSystem.ClearAttackTelegraph();
                    elapsed += DeltaTime();
                }

                if (CanAnimate())
                    await _boss.EffectsSystem.CompleteAttackTelegraph(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!CanContinue() || !_boss.CanAttack)
                    return;

                if (Configuration.Pattern == BossProjectilePattern.ArcingVolley)
                {
                    LaunchVolley(cancellationToken);
                    _boss.PlayCombatSound(Features.Sounds.SoundId.EnemyGhostShot);
                    return;
                }

                HealthSystem health = _boss.HealthSystem;
                float threshold = health.MaxHealth * Mathf.Clamp(Configuration.BurstHealthPercentage, 0f, 100f) * 0.01f;
                int ringCount = health.CurrentHealth <= threshold ? Mathf.Max(1, Configuration.BurstRingCount) : 1;
                Quaternion initialRotation = _boss.AttackRotation;
                for (int ring = 0; ring < ringCount; ring++)
                {
                    if (ring > 0)
                    {
                        float remaining = Mathf.Max(0f, Configuration.RingInterval);
                        while (remaining > 0f)
                        {
                            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                            cancellationToken.ThrowIfCancellationRequested();
                            if (!CanContinue())
                                return;
                            remaining -= DeltaTime();
                        }
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    if (!CanContinue() || !_boss.CanAttack)
                        return;
                    LaunchRing(initialRotation * Quaternion.AngleAxis(ring * Configuration.RingAngleStep, Vector3.up));
                    _boss.PlayCombatSound(Features.Sounds.SoundId.EnemyGhostShot);
                }
            }
            finally
            {
                _executing = false;
                if (_boss != null)
                {
                    _boss.EffectsSystem.ClearAttackTelegraph();
                    StopVelocity();
                    if (!_boss.IsDead && _boss.isActiveAndEnabled && CanAnimate())
                        _boss.AnimationSystem.IdleAnimation();
                }
            }
        }

        private bool CanContinue() => _boss != null && !_boss.IsDead && _boss.isActiveAndEnabled &&
            _boss is not MushroomBossFacade { IsPlayingSplitTransition: true } &&
            _character != null && _character.isActiveAndEnabled && _character.HealthSystem != null &&
            !_character.HealthSystem.IsDead && !_character.IsTransitionPaused;

        private float CurrentTimeScale() =>
            CanContinue() && !_character.IsCinematicPaused ? _boss.RelicTimeScale : 0f;

        private float DeltaTime()
        {
            float timeScale = CurrentTimeScale();
            _boss.AnimationSystem.SetTimeScale(timeScale);
            return Time.deltaTime * timeScale;
        }

        private void FaceCharacter()
        {
            if (_boss is not WoodGuardBossFacade)
            {
                Quaternion rotation = BossFacade.GetFlatRotation(_character.transform.position - _boss.transform.position);
                if (_boss.Rigidbody != null)
                    _boss.Rigidbody.rotation = rotation;
                _boss.transform.rotation = rotation;
            }
            StopVelocity();
        }

        private void StopVelocity()
        {
            Rigidbody body = _boss.Rigidbody;
            if (body == null || body.isKinematic)
                return;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        private void LaunchRing(Quaternion rotation)
        {
            Vector3 origin = _boss.RadialAttackOrigin.position;
            float size = _boss is MushroomBossFacade mushroom ? mushroom.SizeMultiplier : 1f;
            float radius = Mathf.Max(0f, Configuration.SpawnRadius) * size;
            int count = Mathf.Max(3, Configuration.ProjectileCount);
            for (int index = 0; index < count; index++)
            {
                Vector3 direction = rotation * Quaternion.AngleAxis(360f * index / count, Vector3.up) * Vector3.forward;
                MushroomRadialProjectile projectile = CreateProjectile(origin + direction * radius, direction);
                projectile.Launch(_boss, _character, direction, Configuration.ProjectileSpeed,
                    Configuration.ProjectileLifetime, Configuration.Damage);
            }
        }

        private void LaunchVolley(CancellationToken cancellationToken)
        {
            Vector3 origin = _boss.RadialAttackOrigin.position;
            // Capture once: the entire volley can be dodged without the landing points following the player.
            Vector3 target = _character.transform.position;
            Vector3 aim = target - origin;
            aim.y = 0f;
            Vector3 forward = aim.sqrMagnitude > 0.0001f ? aim.normalized :
                _boss.AttackRotation * Vector3.forward;
            float spawnDistance = Mathf.Min(Mathf.Max(0f, Configuration.SpawnRadius), aim.magnitude * 0.5f);
            Vector3 start = origin + forward * spawnDistance;
            aim = target - start;
            aim.y = 0f;
            float distance = Mathf.Max(0.05f, aim.magnitude);
            float halfSpread = Mathf.Clamp(Configuration.VolleySpreadAngle, 0f, 180f) * 0.5f;
            int count = Mathf.Max(3, Configuration.ProjectileCount);
            for (int index = 0; index < count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Vector3 direction = Quaternion.AngleAxis(UnityEngine.Random.Range(-halfSpread, halfSpread),
                    Vector3.up) * forward;
                Vector3 landing = start + direction * (distance * Sample(Configuration.VolleyRangeMultiplier, 0.05f));
                // The player's jump height must not lift the landing plane.
                landing.y = _boss.AttackOrigin.position.y;
                float flightDuration = Sample(Configuration.VolleyFlightDuration, 0.1f);
                float arcHeight = Sample(Configuration.VolleyArcHeight, 0f);
                MushroomRadialProjectile projectile = CreateProjectile(start, direction);
                projectile.LaunchArc(_boss, _character, landing, flightDuration, arcHeight,
                    Configuration.Damage, cancellationToken);
            }
        }

        private MushroomRadialProjectile CreateProjectile(Vector3 position, Vector3 direction)
        {
            GameObject instance = Object.Instantiate(Configuration.ProjectilePrefab,
                position, BossFacade.GetFlatRotation(direction));
            if (instance.TryGetComponent(out MushroomRadialProjectile projectile))
                return projectile;
            Object.Destroy(instance);
            throw new InvalidOperationException(
                "Boss projectile attacks require a prepared prefab with MushroomRadialProjectile on its root.");
        }

        private static float Sample(Vector2 range, float minimum)
        {
            float min = Mathf.Max(minimum, Mathf.Min(range.x, range.y));
            float max = Mathf.Max(min, Mathf.Max(range.x, range.y));
            return UnityEngine.Random.Range(min, max);
        }
    }
}
