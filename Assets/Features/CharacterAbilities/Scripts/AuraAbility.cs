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

    private AuraAbilityConfiguration _configuration;
    private CharacterFacade _owner;
    private AuraDamageArea _aura;
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
        if (_configuration.Prefab == null)
            Debug.LogError("AURA requires a prefab with an AuraDamageArea component.", _configuration);
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
        if (_aura != null)
            _aura.SetRadius(_radius);
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

        // Instantiate the prepared damage-area component together with its configured visual.
        if (_aura == null)
        {
            _aura = Object.Instantiate(_configuration.Prefab, character.transform, false);
            _aura.Initialize(character, _enemiesProvider, _configuration, _radius, ApplyDamage);
        }

        base.Use(character);
    }

    protected override void OnUse(CharacterFacade character)
    {
        CurrentCooldown = GetTickInterval();
        _aura.DamageEnemies();
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
        {
            _aura.gameObject.SetActive(false);
            Object.Destroy(_aura.gameObject);
        }
        _aura = null;
        _owner = null;
    }
}
