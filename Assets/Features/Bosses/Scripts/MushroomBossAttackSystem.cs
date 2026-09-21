using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Features.Bosses.Scripts
{
    public sealed class MushroomBossAttackSystem : BossAttackSystem
    {
        private readonly MushroomBossFacade _boss;
        private readonly CharacterFacade _character;
        private MushroomBossConfiguration _configuration;
        private MushroomBossAnimation _animation;
        private float _headCooldown;
        private float _wait;
        private int _lastClockFrame = -1;
        private bool _executing;
        private bool _hasJumpReservation;
        private Vector3 _reservedJumpDestination;

        internal bool CanAdvanceSplitTransition => CanContinue();

        public MushroomBossAttackSystem(MushroomBossFacade boss, CharacterFacade character)
            : base(boss, character)
        {
            _boss = boss;
            _character = character;
        }

        public override void Initialize()
        {
            _configuration = _boss.MovementConfiguration;
            _animation = _boss.AnimationSystem as MushroomBossAnimation;
            _headCooldown = 0f;
            _wait = Mathf.Max(0f, _boss.Config.InitialAttackDelay) + GetSplitAttackDelay();
            _lastClockFrame = -1;
            _executing = false;
            _hasJumpReservation = false;
            _boss.AnimationSystem.IdleAnimation();
        }

        public override async UniTask Tick(CancellationToken cancellationToken)
        {
            while (_boss != null && !_boss.IsDead)
            {
                cancellationToken.ThrowIfCancellationRequested();
                float delta = AdvanceClock();
                if (CanContinue() && !_boss.IsPlayingSplitTransition && _boss.CanAttack && delta > 0f)
                {
                    _wait = Mathf.Max(0f, _wait - delta);
                    if (_wait <= 0f)
                        await Execute(cancellationToken);
                }
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        public override UniTask Execute(CancellationToken cancellationToken) =>
            ExecuteAttack(null, cancellationToken);

        internal async UniTask ExecuteAttack(MushroomAttackConfiguration requestedAttack,
            CancellationToken cancellationToken, bool animateBoss = true, Func<bool> ownsAnimation = null)
        {
            bool CanAnimate() => animateBoss && (ownsAnimation?.Invoke() ?? true);

            cancellationToken.ThrowIfCancellationRequested();
            if (_executing || _boss.IsPlayingSplitTransition || _configuration == null || _animation == null ||
                !CanContinue() || !_boss.CanAttack || Time.deltaTime <= 0f)
                return;

            MushroomJumpAttackConfiguration jump = requestedAttack == null
                ? FindAttack<MushroomJumpAttackConfiguration>()
                : requestedAttack as MushroomJumpAttackConfiguration;
            MushroomHeadAttackConfiguration head = requestedAttack == null
                ? FindAttack<MushroomHeadAttackConfiguration>()
                : requestedAttack as MushroomHeadAttackConfiguration;
            if (jump != null && !jump.IsEnabled) jump = null;
            if (head != null && !head.IsEnabled) head = null;
            if (jump == null && head == null)
                return;

            Vector3 direction = _character.transform.position - _boss.transform.position;
            direction.y = 0f;
            float distance = direction.magnitude;
            direction = distance > 0.001f ? direction / distance : _boss.AttackRotation * Vector3.forward;
            Face(direction);

            Vector3 start = _boss.transform.position;
            Vector3 destination = start;
            // Capture a fixed point ahead, keeping the floor probe at the boss's base height.
            Vector3 headGroundProbe = start + direction * (head != null ? Mathf.Max(0f, head.ImpactDistance) : 0f);
            Vector3 impactPoint = default;
            bool headAttack = head != null && _headCooldown <= 0f && distance <= Mathf.Max(0f, head.Range) &&
                              TryGetGround(headGroundProbe, out impactPoint);
            if (!headAttack)
            {
                if (jump == null)
                    return;
                bool planned = _boss.IsSplitChild
                    ? TryPlanSplitJump(jump, head, out destination, out impactPoint)
                    : TryPlanJump(direction, distance, jump.Distance, out destination, out impactPoint);
                if (!planned)
                {
                    if (_boss.IsSplitChild)
                        _wait = 0.15f + GetSplitAttackDelay();
                    return;
                }
                Vector3 movement = destination - start;
                movement.y = 0f;
                if (_boss.IsSplitChild && movement.sqrMagnitude > 0.001f)
                {
                    direction = movement.normalized;
                    Face(direction);
                }
            }

            MushroomAttackConfiguration attack = headAttack ? (MushroomAttackConfiguration)head : jump;
            float duration = Mathf.Max(0.01f, attack.Duration);
            float impact = Mathf.Clamp(attack.ImpactNormalized, 0.01f, 1f);
            float takeoff = headAttack ? 0f : Mathf.Clamp(jump.TakeoffNormalized, 0f, impact - 0.001f);
            float radius = Mathf.Max(0.01f, attack.Radius);
            GameObject indicator = null;
            Vector3 indicatorScale = Vector3.one;
            bool hasHit = false;
            float elapsed = 0f;
            _executing = true;
            // Reserve before the first await so the sibling can plan a simultaneous, separate route.
            _hasJumpReservation = _boss.IsSplitChild && !headAttack;
            _reservedJumpDestination = destination;
            _boss.CombatSystem.SetAttacking(true);
            Vector3 headEffectPoint = impactPoint;
            if (headAttack)
            {
                _headCooldown = Mathf.Max(0f, head.Cooldown);
                _boss.SetHeadImpactPoint(impactPoint);
                headEffectPoint.y = _boss.HeadImpactOrigin.position.y;
            }

            try
            {
                GameObject indicatorPrefab = attack.IndicatorPrefab;
                if (indicatorPrefab != null)
                {
                    // No parent: the warning stays at the captured point as the mushroom moves.
                    indicator = Object.Instantiate(indicatorPrefab,
                        impactPoint + Vector3.up * attack.IndicatorGroundOffset, Quaternion.identity);
                    foreach (Collider collider in indicator.GetComponentsInChildren<Collider>(true))
                        collider.enabled = false;
                    indicatorScale = indicator.transform.localScale;
                    UpdateWarning(indicator.transform, indicatorScale, attack, 0f);
                    indicator.SetActive(true);
                }

                if (CanAnimate())
                {
                    if (headAttack)
                        _animation.BeginHead();
                    else
                        _animation.BeginJump();
                    _animation.SampleAttack(0f);
                }

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
                    if (!headAttack)
                    {
                        float flight = Mathf.InverseLerp(takeoff, impact, progress);
                        Vector3 position = Vector3.Lerp(start, destination, flight);
                        // The clip supplies the vertical jump. Move the root only over the floor.
                        position.y = flight >= 1f ? destination.y : start.y;
                        SetPosition(position);
                    }
                    if (CanAnimate())
                        _animation.SampleAttack(progress);

                    if (!hasHit && indicator != null)
                        UpdateWarning(indicator.transform, indicatorScale, attack, progress / impact);
                    if (!hasHit && progress >= impact)
                    {
                        hasHit = true;
                        _hasJumpReservation = false;
                        DestroyIndicator(indicator);
                        indicator = null;
                        Vector3 effectPoint = headAttack
                            ? headEffectPoint
                            : _boss.LandingEffectOrigin.position;
                        SpawnEffect(attack.EffectPrefab, effectPoint, attack.EffectLifetime);
                        ApplyHit(impactPoint, radius, attack, direction);
                        if (!CanContinue() || cancellationToken.IsCancellationRequested)
                            return;
                    }
                }
            }
            finally
            {
                DestroyIndicator(indicator);
                _executing = false;
                _hasJumpReservation = false;
                _wait = Mathf.Max(0f, attack.RecoveryDuration) + GetSplitAttackDelay();
                if (_boss != null)
                {
                    StopVelocity();
                    if (!_boss.IsDead && _boss.isActiveAndEnabled && CanAnimate())
                        _boss.AnimationSystem.IdleAnimation();
                    _boss.CombatSystem.SetAttacking(false);
                }
            }
        }

        private T FindAttack<T>() where T : MushroomAttackConfiguration
        {
            HealthSystem health = _boss.HealthSystem;
            float healthPercentage = health.CurrentHealth / Mathf.Max(1f, health.MaxHealth) * 100f;
            BossAttackConfiguration[] attacks = _boss.Config.GetAttacksForHealth(healthPercentage);
            if (attacks == null)
                return null;
            foreach (BossAttackConfiguration candidate in attacks)
            {
                if (candidate is T attack && attack.IsEnabled)
                    return attack;
            }
            return null;
        }

        private bool CanContinue() => _boss != null && !_boss.IsDead && _boss.isActiveAndEnabled &&
            _character != null && _character.HealthSystem != null && !_character.HealthSystem.IsDead &&
            !_character.IsTransitionPaused;

        private float AdvanceClock()
        {
            float timeScale = CanContinue() && !_boss.IsPlayingSplitTransition ? _boss.RelicTimeScale : 0f;
            _animation?.SetTimeScale(timeScale);
            if (_lastClockFrame == Time.frameCount)
                return 0f;
            _lastClockFrame = Time.frameCount;
            float delta = Time.deltaTime * timeScale;
            _headCooldown = Mathf.Max(0f, _headCooldown - delta);
            return delta;
        }

        internal Vector3 GetSplitPosition(Vector3 direction, float distance) =>
            _configuration != null && TryPlanJump(direction, distance, distance,
                out Vector3 destination, out _) ? destination : _boss.transform.position;

        private float GetSplitAttackDelay()
        {
            if (_boss == null || !_boss.IsSplitChild || _configuration == null)
                return 0f;
            Vector2 range = _configuration.SplitAttackDelayRange;
            float minimum = Mathf.Max(0f, Mathf.Min(range.x, range.y));
            float maximum = Mathf.Max(minimum, Mathf.Max(range.x, range.y));
            return Random.Range(minimum, maximum);
        }

        private MushroomBossFacade GetLivingSibling()
        {
            MushroomBossFacade sibling = _boss.SplitSibling;
            return sibling != null && !sibling.IsDead && sibling.isActiveAndEnabled ? sibling : null;
        }

        private bool TryPlanSplitJump(MushroomJumpAttackConfiguration jump,
            MushroomHeadAttackConfiguration head, out Vector3 destination, out Vector3 groundPoint)
        {
            Vector3 start = _boss.transform.position;
            Vector3 target = _character.transform.position;
            MushroomBossFacade sibling = GetLivingSibling();
            Vector3 center = sibling != null ? (start + sibling.transform.position) * 0.5f : start;
            Vector3 approach = center - target;
            approach.y = 0f;
            approach = approach.sqrMagnitude > 0.001f
                ? approach.normalized : -(_boss.AttackRotation * Vector3.forward);

            // Land beside the player at a useful head-strike distance, on a different side for each child.
            float radius = head != null
                ? Mathf.Min(Mathf.Max(0f, head.ImpactDistance), Mathf.Max(0f, head.Range))
                : Mathf.Max(0f, jump.Radius) * 0.65f;
            radius = Mathf.Max(GetBodyRadius(_boss) + 0.5f, radius);
            float angle = Mathf.Clamp(_configuration.SplitFlankAngle, 10f, 120f) + Random.Range(-10f, 10f);
            float orbitRadius = radius * Random.Range(0.85f, 1f);

            for (int attempt = 0; attempt < 8; attempt++)
            {
                // Try nearby angles on this child's side before taking a wider detour.
                float adjustment = attempt == 0 ? 0f :
                    ((attempt + 1) / 2) * 20f * (attempt % 2 == 0 ? 1f : -1f);
                float flankAngle = Mathf.Clamp(angle + adjustment, 10f, 150f) * _boss.SplitSide;
                Vector3 waypoint = target + Quaternion.AngleAxis(flankAngle, Vector3.up) * approach * orbitRadius;
                Vector3 movement = waypoint - start;
                movement.y = 0f;
                float travel = movement.magnitude;
                if (travel <= 0.05f || !TryPlanJump(movement / travel, travel, jump.Distance,
                        out Vector3 candidate, out Vector3 floor))
                    continue;
                Vector3 actualMovement = candidate - start;
                actualMovement.y = 0f;
                if (actualMovement.sqrMagnitude < 0.01f || !HasSplitJumpClearance(start, candidate, sibling))
                    continue;
                destination = candidate;
                groundPoint = floor;
                return true;
            }

            destination = start;
            groundPoint = start;
            return false;
        }

        private bool HasSplitJumpClearance(Vector3 start, Vector3 destination, MushroomBossFacade sibling)
        {
            if (sibling == null)
                return true;
            Vector3 otherStart = sibling.transform.position;
            Vector3 otherEnd = otherStart;
            if (sibling.AttackSystem is MushroomBossAttackSystem other && other._hasJumpReservation)
                otherEnd = other._reservedJumpDestination;

            float separation = GetBodyRadius(_boss) + GetBodyRadius(sibling) +
                               Mathf.Max(0f, _configuration.SplitSeparationPadding);
            float requiredSquared = separation * separation;
            if (PointSegmentDistanceSquared(destination, otherStart, otherEnd) < requiredSquared)
                return false;

            // Children can spawn inside the extra padding near a wall. Allow only a route moving apart.
            float initialSquared = PointSegmentDistanceSquared(start, otherStart, otherEnd);
            return SegmentDistanceSquared(start, destination, otherStart, otherEnd) + 0.0001f >=
                   Mathf.Min(requiredSquared, initialSquared);
        }

        private static float GetBodyRadius(MushroomBossFacade boss)
        {
            float radius = boss.MovementConfiguration != null
                ? boss.MovementConfiguration.BodyRadius * boss.SizeMultiplier : 0.05f;
            if (boss.IsSplitChild && boss.Collider != null)
                radius = Mathf.Max(radius,
                    Mathf.Max(boss.Collider.bounds.extents.x, boss.Collider.bounds.extents.z));
            return Mathf.Max(0.05f, radius);
        }

        private static float PointSegmentDistanceSquared(Vector3 point, Vector3 start, Vector3 end)
        {
            point.y = start.y = end.y = 0f;
            Vector3 segment = end - start;
            float progress = segment.sqrMagnitude > 0.0001f
                ? Mathf.Clamp01(Vector3.Dot(point - start, segment) / segment.sqrMagnitude) : 0f;
            return (point - start - segment * progress).sqrMagnitude;
        }

        private static float SegmentDistanceSquared(Vector3 start, Vector3 end, Vector3 otherStart, Vector3 otherEnd)
        {
            Vector3 first = end - start;
            Vector3 second = otherEnd - otherStart;
            Vector3 offset = otherStart - start;
            float cross = first.x * second.z - first.z * second.x;
            if (Mathf.Abs(cross) > 0.0001f)
            {
                float firstProgress = (offset.x * second.z - offset.z * second.x) / cross;
                float secondProgress = (offset.x * first.z - offset.z * first.x) / cross;
                if (firstProgress >= 0f && firstProgress <= 1f && secondProgress >= 0f && secondProgress <= 1f)
                    return 0f;
            }
            return Mathf.Min(
                Mathf.Min(PointSegmentDistanceSquared(start, otherStart, otherEnd),
                    PointSegmentDistanceSquared(end, otherStart, otherEnd)),
                Mathf.Min(PointSegmentDistanceSquared(otherStart, start, end),
                    PointSegmentDistanceSquared(otherEnd, start, end)));
        }

        private bool TryPlanJump(Vector3 direction, float targetDistance, float jumpDistance,
            out Vector3 destination, out Vector3 groundPoint)
        {
            destination = _boss.transform.position;
            if (!TryGetGround(_boss.AttackOrigin.position, out groundPoint))
                return false;

            Vector3 startGround = groundPoint;
            float distance = Mathf.Min(Mathf.Max(0f, jumpDistance), targetDistance);
            float bodyRadius = GetBodyRadius(_boss);
            if (distance > 0f)
            {
                Vector3 castOrigin = startGround + Vector3.up * (bodyRadius + 0.05f);
                foreach (RaycastHit hit in Physics.SphereCastAll(castOrigin, bodyRadius, direction,
                             distance, _configuration.MovementObstacleMask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider == null || hit.collider.transform.IsChildOf(_boss.transform))
                        continue;
                    distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - 0.05f));
                }
            }

            // Stop at the last supported floor point, including when the room has a pit or edge.
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / Mathf.Max(0.25f, bodyRadius * 0.5f)));
            for (int step = 1; step <= steps; step++)
            {
                Vector3 candidate = startGround + direction * (distance * step / steps);
                if (!TryGetGround(candidate, out Vector3 floor) ||
                    Mathf.Abs(floor.y - groundPoint.y) > bodyRadius || !HasFooting(floor, bodyRadius))
                    break;
                groundPoint = floor;
            }
            destination += groundPoint - startGround;
            return true;
        }

        private bool HasFooting(Vector3 center, float radius)
        {
            float margin = radius * 0.7f;
            return HasFloorAt(center + Vector3.right * margin, center.y, radius) &&
                   HasFloorAt(center + Vector3.left * margin, center.y, radius) &&
                   HasFloorAt(center + Vector3.forward * margin, center.y, radius) &&
                   HasFloorAt(center + Vector3.back * margin, center.y, radius);
        }

        private bool HasFloorAt(Vector3 position, float height, float tolerance) =>
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

        private void Face(Vector3 direction)
        {
            Quaternion rotation = BossFacade.GetFlatRotation(direction);
            if (_boss.Rigidbody != null)
                _boss.Rigidbody.rotation = rotation;
            _boss.transform.rotation = rotation;
            StopVelocity();
        }

        private void SetPosition(Vector3 position)
        {
            if (_boss.Rigidbody != null)
                _boss.Rigidbody.position = position;
            _boss.transform.position = position;
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

        private static void UpdateWarning(Transform indicator, Vector3 originalScale,
            MushroomAttackConfiguration attack, float progress)
        {
            float growth = Mathf.Lerp(Mathf.Clamp(attack.InitialIndicatorScale, 0.01f, 1f),
                1f, Mathf.Clamp01(progress));
            float radius = Mathf.Max(0.01f, attack.Radius);
            indicator.localScale = new Vector3(originalScale.x * radius * 2f * growth,
                originalScale.y, originalScale.z * radius * 2f * growth);
        }

        private static void SpawnEffect(GameObject prefab, Vector3 position, float lifetime)
        {
            if (prefab == null)
                return;
            GameObject effect = Object.Instantiate(prefab, position, Quaternion.identity);
            Object.Destroy(effect, Mathf.Max(0.1f, lifetime));
        }

        private void ApplyHit(Vector3 ground, float radius, MushroomAttackConfiguration attack,
            Vector3 fallbackDirection)
        {
            if (attack is MushroomJumpAttackConfiguration && _character.MoveSystem?.IsGrounded == false)
                return;

            // A short vertical column has the same circular footprint as the ground warning.
            Collider[] hits = Physics.OverlapCapsule(ground, ground + Vector3.up * radius, radius,
                Physics.AllLayers, QueryTriggerInteraction.Collide);
            foreach (Collider hit in hits)
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
