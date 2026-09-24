using System;
using Cysharp.Threading.Tasks;
using Features.Bosses.Scripts;
using Features.Enemies.Scripts;
using Features.Relics.Scripts;
using UnityEngine;

public sealed class PunchAbility : CharacterActiveAbility
{
    private const int PunchesPerSeries = 3;
    private const int MaxSequentialPunchSlots = 9;
    private const int PunchOverlapCapacity = 64;
    private const float MinimumCooldown = 0.05f;
    private const float GoldenAngle = 137.5f;
    private const float DirectionEpsilon = 0.001f;
    private const float SimultaneousEffectSpread = 0.25f;

    private static readonly int PunchCollisionMask = LayerMask.GetMask("Default", "Enemy");
    private static readonly float[] TargetLateralOffsets = { -0.3f, 0.3f, 0f };
    private static readonly float[] TargetVerticalOffsets = { 0.15f, -0.1f, 0.25f };
    private const string DamageStatName = "Damage";
    private const string RadiusStatName = "Radius";
    private const string SimultaneousAttacksStatName = "Attacks at Once";
    private const string CooldownStatName = "Cooldown";
    private const string SeriesCountStatName = "Series";

    private static readonly AbilityUpgradeType[] PunchUpgradeTypes =
    {
        AbilityUpgradeType.Damage,
        AbilityUpgradeType.PunchRadius,
        AbilityUpgradeType.PunchSimultaneousAttacks,
        AbilityUpgradeType.Cooldown,
        AbilityUpgradeType.AdditionalProjectiles
    };

    private static readonly AbilityUpgradeType[] PunchUpgradeTypesAtMinimumCooldown =
    {
        AbilityUpgradeType.Damage,
        AbilityUpgradeType.PunchRadius,
        AbilityUpgradeType.PunchSimultaneousAttacks,
        AbilityUpgradeType.AdditionalProjectiles
    };

    private readonly IEnemiesProvider _enemiesProvider;
    private readonly CharacterDamageCalculator _damageCalculator;
    private readonly CharacterStats _characterStats;
    private readonly RelicEventBus _relicEventBus;
    private readonly RelicManager _relicManager;
    private readonly Collider[] _punchOverlapResults = new Collider[PunchOverlapCapacity];

    private PunchAbilityConfiguration _configuration;
    private int _damage;
    private float _radius;
    private float _simultaneousAttackCount;
    private float _additionalSeriesCount;
    private bool _isAttacking;
    private int _attackSequence;

    protected override bool StartCooldownImmediately => false;

    public override AbilityUpgradeType[] UpgradeTypes =>
        _configuration != null && Cooldown <= MinimumCooldown + Mathf.Epsilon
            ? PunchUpgradeTypesAtMinimumCooldown
            : PunchUpgradeTypes;

    public PunchAbility(IEnemiesProvider enemiesProvider, CharacterDamageCalculator damageCalculator,
        CharacterStats characterStats, RelicEventBus relicEventBus, RelicManager relicManager)
    {
        _enemiesProvider = enemiesProvider;
        _damageCalculator = damageCalculator;
        _characterStats = characterStats;
        _relicEventBus = relicEventBus;
        _relicManager = relicManager;
    }

    public override void Initialize(AbilityConfiguration abilityConfig)
    {
        _attackSequence++;
        _isAttacking = false;
        base.Initialize(abilityConfig);

        _configuration = (PunchAbilityConfiguration)abilityConfig;
        Level = 0;
        Cooldown = Mathf.Max(MinimumCooldown, _configuration.Cooldown);
        CurrentCooldown = 0f;
        _damage = Mathf.Max(0, _configuration.StartDamage);
        _radius = Mathf.Max(0.1f, _configuration.Radius);
        _simultaneousAttackCount = Mathf.Max(1f, _configuration.StartSimultaneousAttackCount);
        _additionalSeriesCount = 0f;

        StatName_1 = DamageStatName;
        StatName_2 = SeriesCountStatName;
        RefreshStats();
    }

    public override void OnEquip(CharacterStats characterStats)
    {
        base.OnEquip(characterStats);

        ApplyUpgradeEffect(CurrentPrimaryUpgrade);
        if (CurrentSecondaryUpgrade.HasValue)
            ApplyUpgradeEffect(CurrentSecondaryUpgrade.Value);

        RefreshStats();
    }

    public override void OnUnequip(CharacterStats characterStats)
    {
        _attackSequence++;
        _isAttacking = false;
        _additionalSeriesCount = 0f;
        base.OnUnequip(characterStats);
    }

    public override float GetStatFromIncrease() =>
        _damage;

    public override float GetStatToIncrease(float upgradeMultiplier) =>
        GetDamageTo(upgradeMultiplier);

    public override AbilityUpgradePreview[] GetAcquirePreviews() =>
        new[]
        {
            new AbilityUpgradePreview(DamageStatName, _configuration.StartDamage),
            new AbilityUpgradePreview(SeriesCountStatName, 1f)
        };

    public override AbilityUpgradePreview GetUpgradePreview(AbilityUpgradeEffect upgrade)
    {
        if (upgrade.Type == AbilityUpgradeType.PunchRadius)
        {
            return new AbilityUpgradePreview(RadiusStatName, _radius,
                _radius + GetRadiusIncrease(upgrade.Value), "m");
        }

        return upgrade.Type switch
        {
            AbilityUpgradeType.PunchSimultaneousAttacks =>
                new AbilityUpgradePreview(SimultaneousAttacksStatName, GetCurrentSimultaneousAttackCount(),
                    GetCurrentSimultaneousAttackCount() + GetSimultaneousAttackIncrease(upgrade.Value)),
            AbilityUpgradeType.Cooldown =>
                new AbilityUpgradePreview(CooldownStatName, Cooldown,
                    GetCooldownTo(upgrade.Value), "s"),
            AbilityUpgradeType.AdditionalProjectiles =>
                new AbilityUpgradePreview(SeriesCountStatName, GetCurrentSeriesCount(),
                    GetCurrentSeriesCount() + GetAdditionalSeriesIncrease(upgrade.Value)),
            _ => new AbilityUpgradePreview(DamageStatName, _damage,
                GetDamageTo(upgrade.Value))
        };
    }

    protected override bool IsReady(CharacterFacade character) =>
        _isAttacking == false &&
        character != null &&
        _configuration != null &&
        _configuration.Prefab != null;

    protected override void OnUse(CharacterFacade character)
    {
        int seriesCount = CalculateSeriesCount();
        int simultaneousAttackCount = CalculateSimultaneousAttackCount();
        int attackSequence = ++_attackSequence;
        _isAttacking = true;
        LaunchPunchSeries(character, seriesCount, simultaneousAttackCount, attackSequence).Forget();
    }

    private async UniTask LaunchPunchSeries(CharacterFacade character, int seriesCount,
        int simultaneousAttackCount, int attackSequence)
    {
        try
        {
            float punchDelay = Mathf.Max(0f, _configuration.PunchInterval) /
                               Mathf.Max(0.01f, _characterStats.RelicAttackSpeedMultiplier);
            int totalPunchCount = Mathf.Max(1, seriesCount) * PunchesPerSeries;
            int punchSlotCount = Mathf.Min(totalPunchCount, MaxSequentialPunchSlots);
            int attacksAtOnce = Mathf.Max(1, simultaneousAttackCount);
            int globalPunchIndex = 0;
            float idleAngleOffset = UnityEngine.Random.Range(0f, 360f);

            for (int slotIndex = 0; slotIndex < punchSlotCount; slotIndex++)
            {
                if (slotIndex > 0 && punchDelay > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(punchDelay),
                        cancellationToken: character.GetCancellationTokenOnDestroy());
                }

                if (attackSequence != _attackSequence)
                    return;

                int punchesInSlot = GetPunchCountInSlot(totalPunchCount, punchSlotCount, slotIndex);
                int effectsInSlot = punchesInSlot * attacksAtOnce;
                int effectIndexInSlot = 0;

                for (int punchInSlotIndex = 0; punchInSlotIndex < punchesInSlot; punchInSlotIndex++)
                {
                    int punchIndex = globalPunchIndex % PunchesPerSeries;
                    for (int simultaneousIndex = 0; simultaneousIndex < attacksAtOnce; simultaneousIndex++)
                    {
                        int globalEffectIndex = globalPunchIndex * attacksAtOnce + simultaneousIndex;
                        ExecutePunch(character, punchIndex, globalEffectIndex, idleAngleOffset,
                            effectIndexInSlot, effectsInSlot, GetPunchDamage(punchIndex));
                        effectIndexInSlot++;
                    }

                    globalPunchIndex++;
                }
            }
        }
        finally
        {
            if (attackSequence == _attackSequence)
            {
                _isAttacking = false;
                StartCooldown();
            }
        }
    }

    private static int GetPunchCountInSlot(int totalPunchCount, int slotCount, int slotIndex)
    {
        int currentEnd = DivideRoundingUp((slotIndex + 1) * totalPunchCount, slotCount);
        int previousEnd = DivideRoundingUp(slotIndex * totalPunchCount, slotCount);
        return currentEnd - previousEnd;
    }

    private static int DivideRoundingUp(int value, int divisor) =>
        (value + divisor - 1) / divisor;

    private void ExecutePunch(CharacterFacade character, int punchIndex, int globalPunchIndex,
        float idleAngleOffset, int simultaneousIndex, int simultaneousAttackCount, int punchDamage)
    {
        CombatTarget preferredEnemy = GetClosestEnemy(character);
        GetPunchPose(character, preferredEnemy, punchIndex, globalPunchIndex, idleAngleOffset,
            simultaneousIndex, simultaneousAttackCount, out Vector3 punchPosition,
            out Quaternion punchRotation);
        SpawnPunchEffect(punchPosition, punchRotation);

        if (preferredEnemy is BossFacade && preferredEnemy is not MushroomBossFacade)
        {
            ApplyDamage(character, preferredEnemy, punchDamage, punchPosition);
            return;
        }

        if (TryGetCollisionTarget(punchPosition, preferredEnemy,
                out CombatTarget hitEnemy, out Vector3 hitPosition) == false)
            return;

        ApplyDamage(character, hitEnemy, punchDamage, hitPosition);
    }

    private CombatTarget GetClosestEnemy(CharacterFacade character)
    {
        if (character == null)
            return null;

        Vector3 characterPosition = character.transform.position;
        float radius = Mathf.Max(0.1f, _radius);
        float closestSqrDistance = radius * radius;
        CombatTarget closestEnemy = null;

        foreach (CombatTarget enemy in _enemiesProvider.ActiveEnemies)
        {
            if (enemy == null || enemy.gameObject.activeInHierarchy == false || enemy.IsDead)
                continue;

            Vector3 targetPosition;
            if (enemy is MushroomBossFacade)
            {
                Collider targetCollider = GetEnemyCollider(enemy);
                targetPosition = targetCollider != null
                    ? targetCollider.ClosestPoint(characterPosition)
                    : enemy.transform.position;
            }
            else
            {
                targetPosition = enemy is BossFacade boss
                    ? boss.GetClosestProjectileTarget(characterPosition).position
                    : enemy.transform.position;
            }
            float sqrDistance = (targetPosition - characterPosition).sqrMagnitude;
            if (sqrDistance >= closestSqrDistance)
                continue;

            closestSqrDistance = sqrDistance;
            closestEnemy = enemy;
        }

        return closestEnemy;
    }

    private void GetPunchPose(CharacterFacade character, CombatTarget preferredEnemy, int punchIndex,
        int globalPunchIndex, float idleAngleOffset, int simultaneousIndex, int simultaneousAttackCount,
        out Vector3 position, out Quaternion rotation)
    {
        if (preferredEnemy != null)
        {
            GetTargetPunchPose(character, preferredEnemy, punchIndex, simultaneousIndex,
                simultaneousAttackCount, out position, out rotation);
            return;
        }

        GetIdlePunchPose(character, globalPunchIndex, idleAngleOffset, out position, out rotation);
    }

    private void GetTargetPunchPose(CharacterFacade character, CombatTarget enemy, int punchIndex,
        int simultaneousIndex, int simultaneousAttackCount, out Vector3 position,
        out Quaternion rotation)
    {
        if (enemy is BossFacade && enemy is not MushroomBossFacade)
        {
            position = enemy.GetNextProjectileTarget().position;
            rotation = GetSafeRotation(position - character.ProjectileSpawnPosition,
                character.transform.forward);
            return;
        }

        Collider targetCollider = GetEnemyCollider(enemy);
        if (enemy is MushroomBossFacade && targetCollider != null)
        {
            GetMushroomPunchPose(character, targetCollider, punchIndex, simultaneousIndex,
                simultaneousAttackCount, out position, out rotation);
            return;
        }

        Transform targetPoint = enemy.TargetToShootDamage != null
            ? enemy.TargetToShootDamage
            : enemy.transform;
        Vector3 targetPosition = targetPoint.position;
        Vector3 towardCharacter = character.ProjectileSpawnPosition - targetPosition;
        towardCharacter.y = 0f;

        if (towardCharacter.sqrMagnitude <= DirectionEpsilon)
            towardCharacter = -character.transform.forward;

        towardCharacter.Normalize();

        if (targetCollider != null)
        {
            float probeDistance = targetCollider.bounds.extents.magnitude +
                                  Mathf.Max(1f, _configuration.ImpactRadius);
            Vector3 surfaceProbe = targetPosition + towardCharacter * probeDistance;
            position = targetCollider.ClosestPoint(surfaceProbe);
        }
        else
        {
            position = targetPosition + towardCharacter;
        }

        Vector3 tangent = Vector3.Cross(Vector3.up, towardCharacter).normalized;
        int offsetIndex = Mathf.Clamp(punchIndex, 0, PunchesPerSeries - 1);
        position += towardCharacter * _configuration.VisibleEffectOffset;
        position += tangent * TargetLateralOffsets[offsetIndex];
        position += Vector3.up * TargetVerticalOffsets[offsetIndex];

        if (simultaneousAttackCount > 1)
        {
            float spreadAngle = (360f * simultaneousIndex / simultaneousAttackCount + punchIndex * 60f) *
                                Mathf.Deg2Rad;
            position += tangent * (Mathf.Cos(spreadAngle) * SimultaneousEffectSpread);
            position += Vector3.up * (Mathf.Sin(spreadAngle) * SimultaneousEffectSpread);
        }

        Vector3 punchDirection = targetPosition - position;
        rotation = GetSafeRotation(punchDirection, -towardCharacter);
    }

    private void GetMushroomPunchPose(CharacterFacade character, Collider targetCollider, int punchIndex,
        int simultaneousIndex, int simultaneousAttackCount, out Vector3 position, out Quaternion rotation)
    {
        Bounds characterBounds = character.Collider != null
            ? character.Collider.bounds
            : new Bounds(character.ProjectileSpawnPosition, Vector3.zero);
        Bounds targetBounds = targetCollider.bounds;
        float minimumHeight = characterBounds.center.y;
        float maximumHeight = characterBounds.max.y;
        float overlapMinimum = Mathf.Max(minimumHeight, targetBounds.min.y);
        float overlapMaximum = Mathf.Min(maximumHeight, targetBounds.max.y);
        float height = overlapMinimum <= overlapMaximum
            ? UnityEngine.Random.Range(overlapMinimum, overlapMaximum)
            : UnityEngine.Random.Range(minimumHeight, maximumHeight);

        Vector3 towardCharacter = characterBounds.center - targetBounds.center;
        towardCharacter.y = 0f;
        if (towardCharacter.sqrMagnitude <= DirectionEpsilon)
        {
            towardCharacter = -character.transform.forward;
            towardCharacter.y = 0f;
        }
        towardCharacter = towardCharacter.sqrMagnitude > DirectionEpsilon
            ? towardCharacter.normalized : Vector3.back;
        Vector3 tangent = Vector3.Cross(Vector3.up, towardCharacter);
        float lateralOffset = TargetLateralOffsets[Mathf.Clamp(punchIndex, 0, PunchesPerSeries - 1)];
        if (simultaneousAttackCount > 1)
        {
            float angle = (360f * simultaneousIndex / simultaneousAttackCount + punchIndex * 60f) *
                          Mathf.Deg2Rad;
            lateralOffset += Mathf.Cos(angle) * SimultaneousEffectSpread;
        }

        float probeDistance = targetBounds.extents.magnitude + Mathf.Max(1f, _configuration.ImpactRadius);
        Vector3 probe = targetBounds.center + towardCharacter * probeDistance;
        probe.y = height;
        // Probe horizontally so a curved collider cannot pull the hit above Mr Pocket's height band.
        if (targetCollider.Raycast(new Ray(probe + tangent * lateralOffset, -towardCharacter),
                out RaycastHit hit, probeDistance * 2f) ||
            targetCollider.Raycast(new Ray(probe, -towardCharacter), out hit, probeDistance * 2f))
        {
            position = hit.point;
        }
        else
        {
            // Airborne targets may be out of reach; the normal overlap test decides whether this hits.
            position = targetCollider.ClosestPoint(probe);
        }
        position.y = Mathf.Clamp(position.y, minimumHeight, maximumHeight);
        position += towardCharacter * _configuration.VisibleEffectOffset;
        rotation = GetSafeRotation(-towardCharacter, character.transform.forward);
    }

    private void GetIdlePunchPose(CharacterFacade character, int globalPunchIndex,
        float idleAngleOffset, out Vector3 position, out Quaternion rotation)
    {
        float angle = (idleAngleOffset + globalPunchIndex * GoldenAngle) * Mathf.Deg2Rad;
        Vector3 radialDirection = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        float radiusProgress = Mathf.Repeat((globalPunchIndex + 1) * 0.618034f, 1f);
        float distance = Mathf.Lerp(_configuration.IdleEffectMinDistance,
            _configuration.IdleEffectMaxDistance, radiusProgress);
        float heightOffset = (globalPunchIndex % PunchesPerSeries - 1) * 0.15f;
        Vector3 center = character.ProjectileSpawnPosition;

        position = center + radialDirection * distance + Vector3.up * heightOffset;
        rotation = GetSafeRotation(position - center, radialDirection);
    }

    private bool TryGetCollisionTarget(Vector3 punchPosition, CombatTarget preferredEnemy,
        out CombatTarget hitEnemy, out Vector3 hitPosition)
    {
        hitEnemy = null;
        hitPosition = punchPosition;
        float closestSqrDistance = float.PositiveInfinity;
        bool preferredEnemyFound = false;
        int hitCount = Physics.OverlapSphereNonAlloc(punchPosition,
            Mathf.Max(0.01f, _configuration.ImpactRadius), _punchOverlapResults,
            PunchCollisionMask, QueryTriggerInteraction.Ignore);

        for (int index = 0; index < hitCount; index++)
        {
            Collider hitCollider = _punchOverlapResults[index];
            _punchOverlapResults[index] = null;
            if (hitCollider == null)
                continue;

            CombatTarget enemy = hitCollider.GetComponentInParent<CombatTarget>();
            if (enemy == null || enemy.IsDead || enemy.gameObject.activeInHierarchy == false)
                continue;

            Vector3 closestPoint = hitCollider.ClosestPoint(punchPosition);
            if (enemy == preferredEnemy)
            {
                hitEnemy = enemy;
                hitPosition = closestPoint;
                preferredEnemyFound = true;
                continue;
            }

            if (preferredEnemyFound)
                continue;

            float sqrDistance = (closestPoint - punchPosition).sqrMagnitude;
            if (sqrDistance >= closestSqrDistance)
                continue;

            closestSqrDistance = sqrDistance;
            hitEnemy = enemy;
            hitPosition = closestPoint;
        }

        return hitEnemy != null;
    }

    private static Collider GetEnemyCollider(CombatTarget enemy)
    {
        if (enemy == null)
            return null;

        if (enemy is MushroomBossFacade mushroom && mushroom.Collider != null &&
            mushroom.Collider.enabled && mushroom.Collider.gameObject.activeInHierarchy)
            return mushroom.Collider;

        Collider collider = enemy.GetComponent<Collider>();
        if (collider != null && collider.enabled)
            return collider;

        return enemy.GetComponentInChildren<Collider>();
    }

    private static Quaternion GetSafeRotation(Vector3 direction, Vector3 fallbackDirection)
    {
        if (direction.sqrMagnitude <= DirectionEpsilon)
            direction = fallbackDirection;
        if (direction.sqrMagnitude <= DirectionEpsilon)
            direction = Vector3.forward;

        return Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private void SpawnPunchEffect(Vector3 position, Quaternion rotation)
    {
        if (_configuration.Prefab == null)
            return;

        GameObject effect = UnityEngine.Object.Instantiate(_configuration.Prefab,
            position, rotation);
        effect.transform.localScale *= 1.44f;
        float duration = Mathf.Max(0.01f, _configuration.EffectDuration * AbilityDurationMultiplier);
        UnityEngine.Object.Destroy(effect, duration);
    }

    private void ApplyDamage(CharacterFacade character, CombatTarget enemy, int baseDamage, Vector3 hitPosition)
    {
        if (character == null || enemy == null || enemy.IsDead || baseDamage <= 0)
            return;

        CharacterDamageResult damageResult = _damageCalculator.Calculate(GetRolledDamage(baseDamage));
        int finalDamage = _relicManager.ModifyOutgoingDamage(damageResult.Damage, enemy);
        int appliedDamage = enemy.HealthSystem.GetDamage(finalDamage, damageResult.IsCritical);
        bool killedByHit = enemy.IsDead;

        if (appliedDamage <= 0)
            return;

        float lifeStealPercent = Mathf.Max(0f, _characterStats.LifeSteal) * 0.01f;
        float healed = character.HealthSystem.IncreaseCurrentHealth(appliedDamage * lifeStealPercent);
        if (healed > 0f)
            _relicEventBus.PublishHeal(new RelicHealEvent(character, healed));

        enemy.EffectsSystem.DealDamage();
        _relicEventBus.PublishHit(new RelicHitEvent(character, enemy, finalDamage,
            damageResult.IsCritical, Id.ToString(), hitPosition, this, appliedDamage));

        if (killedByHit)
            _relicEventBus.PublishKill(new RelicKillEvent(character, enemy, hitPosition, Id.ToString()));
    }

    private int GetPunchDamage(int punchIndex)
    {
        int totalDamage = Mathf.Max(0, _damage);
        int damagePerPunch = totalDamage / PunchesPerSeries;
        int remainder = totalDamage % PunchesPerSeries;
        return damagePerPunch + (punchIndex < remainder ? 1 : 0);
    }

    private int GetRolledDamage(int baseDamage)
    {
        float variation = Mathf.Max(0f, _configuration.DamageVariationPercent) * 0.01f;
        float multiplier = UnityEngine.Random.Range(1f - variation, 1f + variation);
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * multiplier));
    }

    private int CalculateSeriesCount()
    {
        float seriesBonus = Mathf.Max(0f, _characterStats.ProjectileCount) +
                            Mathf.Max(0f, _additionalSeriesCount);
        int seriesCount = 1 + Mathf.FloorToInt(seriesBonus);
        float fractionalSeries = seriesBonus - Mathf.Floor(seriesBonus);

        if (UnityEngine.Random.value < fractionalSeries)
            seriesCount++;

        return Mathf.Max(1, seriesCount);
    }

    private float GetCurrentSeriesCount() =>
        1f + Mathf.Max(0f, _characterStats.ProjectileCount) + Mathf.Max(0f, _additionalSeriesCount);

    private int CalculateSimultaneousAttackCount()
    {
        float attackCount = GetCurrentSimultaneousAttackCount();
        int simultaneousAttackCount = Mathf.FloorToInt(attackCount);
        float fractionalAttack = attackCount - simultaneousAttackCount;

        if (UnityEngine.Random.value < fractionalAttack)
            simultaneousAttackCount++;

        return Mathf.Max(1, simultaneousAttackCount);
    }

    private float GetCurrentSimultaneousAttackCount() =>
        Mathf.Max(1f, _simultaneousAttackCount);

    private void ApplyUpgradeEffect(AbilityUpgradeEffect upgrade)
    {
        if (upgrade.Type == AbilityUpgradeType.PunchRadius)
        {
            _radius += GetRadiusIncrease(upgrade.Value);
            return;
        }

        switch (upgrade.Type)
        {
            case AbilityUpgradeType.Damage:
                _damage += GetDamageIncrease(upgrade.Value);
                break;
            case AbilityUpgradeType.PunchSimultaneousAttacks:
                _simultaneousAttackCount += GetSimultaneousAttackIncrease(upgrade.Value);
                break;
            case AbilityUpgradeType.Cooldown:
                Cooldown = GetCooldownTo(upgrade.Value);
                break;
            case AbilityUpgradeType.AdditionalProjectiles:
                _additionalSeriesCount += GetAdditionalSeriesIncrease(upgrade.Value);
                break;
        }
    }

    private int GetDamageIncrease(float upgradeMultiplier) =>
        Mathf.Max(0,
            Mathf.RoundToInt(GetUpgradeValue(_configuration.DamageUpgradeIncrease, upgradeMultiplier)));

    private int GetDamageTo(float upgradeMultiplier) =>
        _damage + GetDamageIncrease(upgradeMultiplier);

    private float GetRadiusIncrease(float upgradeMultiplier) =>
        GetUpgradeValue(_configuration.RadiusUpgradeIncrease, upgradeMultiplier);

    private float GetSimultaneousAttackIncrease(float upgradeMultiplier) =>
        GetUpgradeValue(_configuration.SimultaneousAttackUpgradeIncrease, upgradeMultiplier);

    private float GetCooldownTo(float upgradeMultiplier) =>
        Mathf.Max(MinimumCooldown,
            Cooldown - GetUpgradeValue(_configuration.CooldownUpgradeReduction, upgradeMultiplier));

    private static float GetAdditionalSeriesIncrease(float upgradeMultiplier) =>
        Mathf.Max(0f, upgradeMultiplier);

    private void RefreshStats()
    {
        Stat_1 = _damage;
        Stat_2 = GetCurrentSeriesCount();
    }
}
