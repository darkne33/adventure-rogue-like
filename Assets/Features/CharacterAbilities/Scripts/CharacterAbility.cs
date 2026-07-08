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
    protected float CurrentUpgradeMultiplier { get; private set; } = 1f;
    protected AbilityUpgradeType CurrentUpgradeType { get; private set; } = AbilityUpgradeType.Default;

    public virtual AbilityUpgradeType[] UpgradeTypes => DefaultUpgradeTypes;

    public AbilityUpgradeType GetRandomUpgradeType()
    {
        AbilityUpgradeType[] upgradeTypes = UpgradeTypes;
        return upgradeTypes is { Length: > 0 }
            ? upgradeTypes[Random.Range(0, upgradeTypes.Length)]
            : AbilityUpgradeType.Default;
    }

    public void ApplyUpgrade(CharacterStats characterStats, float upgradeMultiplier,
        AbilityUpgradeType upgradeType = AbilityUpgradeType.Default)
    {
        CurrentUpgradeMultiplier = Mathf.Max(0f, upgradeMultiplier);
        CurrentUpgradeType = upgradeType;
        _appliedUpgrades.Push(new AppliedAbilityUpgrade(CurrentUpgradeMultiplier, CurrentUpgradeType));
        OnEquip(characterStats);
        IsAcquired = true;
        CurrentUpgradeMultiplier = 1f;
        CurrentUpgradeType = AbilityUpgradeType.Default;
    }

    public virtual void OnEquip(CharacterStats characterStats) =>
        Level++;

    public virtual void OnUnequip(CharacterStats characterStats)
    {
        AppliedAbilityUpgrade appliedUpgrade = _appliedUpgrades.Count > 0
            ? _appliedUpgrades.Pop()
            : new AppliedAbilityUpgrade(1f, AbilityUpgradeType.Default);
        CurrentUpgradeMultiplier = appliedUpgrade.Multiplier;
        CurrentUpgradeType = appliedUpgrade.UpgradeType;
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
}

public readonly struct AppliedAbilityUpgrade
{
    public AppliedAbilityUpgrade(float multiplier, AbilityUpgradeType upgradeType)
    {
        Multiplier = multiplier;
        UpgradeType = upgradeType;
    }

    public float Multiplier { get; }
    public AbilityUpgradeType UpgradeType { get; }
}
