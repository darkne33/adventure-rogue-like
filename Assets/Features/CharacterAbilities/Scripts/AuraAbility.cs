using System.Collections.Generic;
using Features.Enemies.Scripts;
using Features.Relics.Scripts;
using UnityEngine;

public sealed class AuraAbility : CharacterActiveAbility
{
    private const float MinimumEffectiveTickInterval = 0.05f;
    private const string DamageStatName = "Damage";
    private const string TickIntervalStatName = "Tick Interval";
    private const string RadiusStatName = "Radius";

    private static readonly AbilityUpgradeType[] AuraUpgradeTypes =
    {
        AbilityUpgradeType.Damage,
        AbilityUpgradeType.Cooldown,
        AbilityUpgradeType.AuraRadius
    };

    private static readonly AbilityUpgradeType[] AuraUpgradeTypesAtMinimumInterval =
    {
        AbilityUpgradeType.Damage,
        AbilityUpgradeType.AuraRadius
    };

    private readonly IEnemiesProvider _enemiesProvider;
    private readonly CharacterDamageCalculator _damageCalculator;
    private readonly CharacterStats _characterStats;
    private readonly RelicEventBus _relicEventBus;
    private readonly RelicManager _relicManager;
    private readonly List<CombatTarget> _enemiesInRange = new();

    private AuraAbilityConfiguration _configuration;
    private CharacterFacade _owner;
    private GameObject _aura;
    private int _damage;
    private float _radius;

    protected override bool StartCooldownImmediately => false;

    private float MinimumTickInterval =>
        Mathf.Max(MinimumEffectiveTickInterval, _configuration.MinimumTickInterval);

    public override AbilityUpgradeType[] UpgradeTypes =>
        _configuration != null && Cooldown <= MinimumTickInterval + Mathf.Epsilon
            ? AuraUpgradeTypesAtMinimumInterval
            : AuraUpgradeTypes;

    public AuraAbility(IEnemiesProvider enemiesProvider, CharacterDamageCalculator damageCalculator,
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
        CleanupAura();
        base.Initialize(abilityConfig);
        _configuration = (AuraAbilityConfiguration)abilityConfig;
        Level = 0;
        _damage = Mathf.Max(1, _configuration.StartDamage);
        _radius = Mathf.Max(0.1f, _configuration.DamageRadius);
        Cooldown = Mathf.Max(MinimumTickInterval, _configuration.DamageTickInterval);
        CurrentCooldown = 0f;
        StatName_1 = DamageStatName;
        StatName_2 = RadiusStatName;
        RefreshStats();
    }

    public override void OnEquip(CharacterStats characterStats)
    {
        base.OnEquip(characterStats);
        ApplyUpgradeEffect(CurrentPrimaryUpgrade);
        if (CurrentSecondaryUpgrade.HasValue)
            ApplyUpgradeEffect(CurrentSecondaryUpgrade.Value);
        RefreshStats();
        UpdateVisual();
    }

    public override void OnUnequip(CharacterStats characterStats)
    {
        CleanupAura();
        CurrentCooldown = 0f;
        base.OnUnequip(characterStats);
    }

    public override void Use(CharacterFacade character)
    {
        if (character == null || character.HealthSystem.IsDead)
        {
            CleanupAura();
            return;
        }

        if (_configuration.Prefab == null)
            return;

        if (_owner != character)
        {
            CleanupAura();
            _owner = character;
            CurrentCooldown = 0f;
        }

        // Keep the visual attached between damage ticks; destroying the owner also destroys it.
        if (_aura == null)
            _aura = Object.Instantiate(_configuration.Prefab, character.transform.position,
                Quaternion.identity, character.transform);

        UpdateVisual();
        base.Use(character);
    }

    protected override void OnUse(CharacterFacade character)
    {
        CurrentCooldown = GetTickInterval();
        _enemiesInRange.Clear();
        IReadOnlyList<CombatTarget> activeEnemies = _enemiesProvider.ActiveEnemies;
        Vector3 center = character.transform.position;
        float radiusSqr = _radius * _radius;
        float height = Mathf.Max(0.1f, _configuration.DamageHeight);

        // Damage callbacks can remove enemies or spawn new ones, so collect targets first.
        for (int index = 0; index < activeEnemies.Count; index++)
        {
            CombatTarget enemy = activeEnemies[index];
            if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.IsDead)
                continue;

            Vector3 offset = enemy.transform.position - center;
            if (Mathf.Abs(offset.y) > height)
                continue;
            offset.y = 0f;
            if (offset.sqrMagnitude <= radiusSqr)
                _enemiesInRange.Add(enemy);
        }

        foreach (CombatTarget enemy in _enemiesInRange)
            ApplyDamage(character, enemy);
        _enemiesInRange.Clear();
    }

    public override float CalculateEstimatedDps() =>
        _configuration == null || _configuration.Prefab == null
            ? 0f
            : _damageCalculator.CalculateAverageDamage(_damage) / GetTickInterval();

    public override float GetStatFromIncrease() => _damage;

    public override float GetStatToIncrease(float upgradeMultiplier) =>
        _damage + GetDamageIncrease(upgradeMultiplier);

    public override AbilityUpgradePreview[] GetAcquirePreviews() =>
        new[]
        {
            new AbilityUpgradePreview(DamageStatName, _damage),
            new AbilityUpgradePreview(RadiusStatName, _radius, "m")
        };

    public override AbilityUpgradePreview GetUpgradePreview(AbilityUpgradeEffect upgrade) =>
        upgrade.Type switch
        {
            AbilityUpgradeType.Cooldown => new AbilityUpgradePreview(TickIntervalStatName,
                Cooldown, GetTickIntervalTo(upgrade.Value), "s"),
            AbilityUpgradeType.AuraRadius => new AbilityUpgradePreview(RadiusStatName,
                _radius, _radius + GetRadiusIncrease(upgrade.Value), "m"),
            _ => new AbilityUpgradePreview(DamageStatName, _damage,
                _damage + GetDamageIncrease(upgrade.Value))
        };

    private void ApplyUpgradeEffect(AbilityUpgradeEffect upgrade)
    {
        switch (upgrade.Type)
        {
            case AbilityUpgradeType.Damage:
                _damage += GetDamageIncrease(upgrade.Value);
                break;
            case AbilityUpgradeType.Cooldown:
                float remainingFraction = Mathf.Clamp01(CurrentCooldown / GetTickInterval());
                Cooldown = GetTickIntervalTo(upgrade.Value);
                CurrentCooldown = remainingFraction * GetTickInterval();
                break;
            case AbilityUpgradeType.AuraRadius:
                _radius += GetRadiusIncrease(upgrade.Value);
                break;
        }
    }

    private void ApplyDamage(CharacterFacade character, CombatTarget enemy)
    {
        if (character == null || character.HealthSystem.IsDead || enemy == null ||
            !enemy.gameObject.activeInHierarchy || enemy.IsDead)
            return;

        Vector3 hitPosition = enemy.transform.position;
        float variation = Mathf.Clamp01(_configuration.DamageVariationPercent * 0.01f);
        int rolledDamage = Mathf.Max(1, Mathf.RoundToInt(_damage * Random.Range(1f - variation, 1f + variation)));
        CharacterDamageResult damageResult = _damageCalculator.Calculate(rolledDamage);
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

    private void UpdateVisual()
    {
        if (_aura == null || _owner == null)
            return;

        _aura.transform.SetPositionAndRotation(
            _owner.transform.position + Vector3.up * _configuration.VisualHeightOffset,
            Quaternion.identity);
        float scale = _radius / Mathf.Max(0.01f, _configuration.VisualBaseRadius);
        Vector3 ownerScale = _owner.transform.lossyScale;
        _aura.transform.localScale = new Vector3(
            scale / Mathf.Max(0.0001f, Mathf.Abs(ownerScale.x)),
            scale / Mathf.Max(0.0001f, Mathf.Abs(ownerScale.y)),
            scale / Mathf.Max(0.0001f, Mathf.Abs(ownerScale.z)));
    }

    private float GetTickInterval() =>
        Mathf.Max(MinimumEffectiveTickInterval, GetModifiedCooldown());

    private float GetTickIntervalTo(float upgradeMultiplier) =>
        Mathf.Max(MinimumTickInterval,
            Cooldown - GetUpgradeValue(_configuration.TickIntervalUpgradeReduction, upgradeMultiplier));

    private int GetDamageIncrease(float upgradeMultiplier) =>
        Mathf.Max(0, Mathf.RoundToInt(GetUpgradeValue(_configuration.DamageUpgradeIncrease, upgradeMultiplier)));

    private float GetRadiusIncrease(float upgradeMultiplier) =>
        GetUpgradeValue(_configuration.RadiusUpgradeIncrease, upgradeMultiplier);

    private void RefreshStats()
    {
        Stat_1 = _damage;
        Stat_2 = _radius;
    }

    private void CleanupAura()
    {
        if (_aura != null)
            Object.Destroy(_aura);
        _aura = null;
        _owner = null;
        _enemiesInRange.Clear();
    }
}
