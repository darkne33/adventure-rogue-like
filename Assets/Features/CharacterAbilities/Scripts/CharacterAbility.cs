using System.Collections.Generic;
using UnityEngine;

public abstract class CharacterAbility
{
    private static readonly AbilityUpgradeType[] DefaultUpgradeTypes = { AbilityUpgradeType.Default };

    private readonly Stack<AppliedAbilityUpgrade> _appliedUpgrades = new();

    public AbilityName Id { get; protected set; }
    public string DisplayName { get; protected set; }
    public string Description { get; protected set; }
    public Sprite Icon { get; protected set; }
    public int Level { get; protected set; } = 1;
    public bool IsAcquired { get; private set; }
    protected AbilityUpgradeEffect CurrentPrimaryUpgrade { get; private set; } =
        new(AbilityUpgradeType.Default, 1f);
    protected AbilityUpgradeEffect? CurrentSecondaryUpgrade { get; private set; }
    protected float CurrentUpgradeMultiplier => CurrentPrimaryUpgrade.Value;
    protected AbilityUpgradeType CurrentUpgradeType => CurrentPrimaryUpgrade.Type;

    public virtual AbilityUpgradeType[] UpgradeTypes => DefaultUpgradeTypes;

    public AbilityUpgradeType GetRandomUpgradeType()
    {
        AbilityUpgradeType[] upgradeTypes = UpgradeTypes;
        return upgradeTypes is { Length: > 0 }
            ? upgradeTypes[Random.Range(0, upgradeTypes.Length)]
            : AbilityUpgradeType.Default;
    }

    public void ApplyUpgrade(CharacterStats characterStats, float upgradeMultiplier,
        AbilityUpgradeType upgradeType = AbilityUpgradeType.Default) =>
        ApplyUpgrade(characterStats, new AbilityUpgradeEffect(upgradeType, upgradeMultiplier), null);

    public void ApplyUpgrade(CharacterStats characterStats, AbilityUpgradeEffect primaryUpgrade,
        AbilityUpgradeEffect? secondaryUpgrade)
    {
        CurrentPrimaryUpgrade = SanitizeUpgrade(primaryUpgrade);
        CurrentSecondaryUpgrade = secondaryUpgrade.HasValue
            ? SanitizeUpgrade(secondaryUpgrade.Value)
            : null;
        _appliedUpgrades.Push(new AppliedAbilityUpgrade(CurrentPrimaryUpgrade, CurrentSecondaryUpgrade));
        OnEquip(characterStats);
        IsAcquired = true;
        CurrentPrimaryUpgrade = new AbilityUpgradeEffect(AbilityUpgradeType.Default, 1f);
        CurrentSecondaryUpgrade = null;
    }

    public virtual void OnEquip(CharacterStats characterStats) =>
        Level++;

    public virtual void OnUnequip(CharacterStats characterStats)
    {
        AppliedAbilityUpgrade appliedUpgrade = _appliedUpgrades.Count > 0
            ? _appliedUpgrades.Pop()
            : new AppliedAbilityUpgrade(new AbilityUpgradeEffect(AbilityUpgradeType.Default, 1f), null);
        CurrentPrimaryUpgrade = appliedUpgrade.PrimaryUpgrade;
        CurrentSecondaryUpgrade = appliedUpgrade.SecondaryUpgrade;
        Level--;
        IsAcquired = false;
    }

    public virtual void Initialize(AbilityConfiguration abilityConfig)
    {
        Id = abilityConfig.AbilityName;
        DisplayName = abilityConfig.DisplayName;
        Description = abilityConfig.Description;
        Icon = abilityConfig.Icon;
    }

    protected float GetCurrentUpgradeValue(float value) =>
        GetUpgradeValue(value, CurrentUpgradeMultiplier);

    protected float GetUpgradeValue(float value, float upgradeMultiplier) =>
        value * Mathf.Max(0f, upgradeMultiplier);

    private static AbilityUpgradeEffect SanitizeUpgrade(AbilityUpgradeEffect upgrade) =>
        new(upgrade.Type, Mathf.Max(0f, upgrade.Value));
}

public readonly struct AppliedAbilityUpgrade
{
    public AppliedAbilityUpgrade(float multiplier, AbilityUpgradeType upgradeType)
        : this(new AbilityUpgradeEffect(upgradeType, multiplier), null)
    {
    }

    public AppliedAbilityUpgrade(AbilityUpgradeEffect primaryUpgrade,
        AbilityUpgradeEffect? secondaryUpgrade)
    {
        PrimaryUpgrade = primaryUpgrade;
        SecondaryUpgrade = secondaryUpgrade;
    }

    public AbilityUpgradeEffect PrimaryUpgrade { get; }
    public AbilityUpgradeEffect? SecondaryUpgrade { get; }
    public float Multiplier => PrimaryUpgrade.Value;
    public AbilityUpgradeType UpgradeType => PrimaryUpgrade.Type;
}
