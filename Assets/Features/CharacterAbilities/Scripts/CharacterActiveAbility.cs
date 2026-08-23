using System;
using UnityEngine;

public abstract class CharacterActiveAbility : CharacterAbility
{
    private const float PERCENT_MULTIPLIER = 0.01f;
    private const float CooldownReductionCap = 80f;

    private CharacterStats _characterStats;

    public float Cooldown { get; protected set; }
    public float CurrentCooldown { get; set; }

    public float Stat_1 { get; protected set; }
    public string StatName_1 { get; protected set; }

    public float Stat_2 { get; protected set; }
    public string StatName_2 { get; protected set; }

    protected virtual bool StartCooldownImmediately => true;

    public virtual bool CanUse(CharacterFacade character) =>
        CurrentCooldown <= 0f && IsReady(character);

    protected virtual bool IsReady(CharacterFacade character) => true;

    public virtual void Use(CharacterFacade character)
    {
        if (!CanUse(character))
            return;

        OnUse(character);

        if (StartCooldownImmediately)
            StartCooldown();
    }

    protected void StartCooldown()
    {
        float attackSpeedMultiplier =
            1f + Mathf.Max(-90f, _characterStats?.AttackSpeed ?? 0f) * PERCENT_MULTIPLIER;
        float cooldownReductionMultiplier =
            1f - Mathf.Clamp(_characterStats?.CooldownReduction ?? 0f, 0f, CooldownReductionCap) * PERCENT_MULTIPLIER;
        CurrentCooldown = Cooldown * cooldownReductionMultiplier / attackSpeedMultiplier;
    }

    public override void OnEquip(CharacterStats characterStats)
    {
        base.OnEquip(characterStats);
        _characterStats = characterStats;
    }

    protected float AbilityDurationMultiplier =>
        1f + Mathf.Max(0f, _characterStats?.AbilityDuration ?? 0f) * PERCENT_MULTIPLIER;

    protected abstract void OnUse(CharacterFacade character);

    public abstract float GetStatFromIncrease();

    public float GetStatToIncrease() =>
        GetStatToIncrease(1f);

    public abstract float GetStatToIncrease(float upgradeMultiplier);
    public virtual AbilityUpgradePreview[] GetAcquirePreviews() =>
        Array.Empty<AbilityUpgradePreview>();

    public AbilityUpgradePreview[] GetUpgradePreviews(AbilityUpgradeEffect primaryUpgrade,
        AbilityUpgradeEffect secondaryUpgrade) =>
        new[]
        {
            GetUpgradePreview(primaryUpgrade),
            GetUpgradePreview(secondaryUpgrade)
        };

    public abstract AbilityUpgradePreview GetUpgradePreview(AbilityUpgradeEffect upgrade);
}

public readonly struct AbilityUpgradePreview
{
    public AbilityUpgradePreview(string statName, float statFrom, float statTo, string suffix = "")
    {
        StatName = statName;
        StatFrom = statFrom;
        StatTo = statTo;
        Suffix = suffix;
        HasStatFrom = true;
    }

    public AbilityUpgradePreview(string statName, float statTo, string suffix = "")
    {
        StatName = statName;
        StatFrom = 0f;
        StatTo = statTo;
        Suffix = suffix;
        HasStatFrom = false;
    }

    public string StatName { get; }
    public float StatFrom { get; }
    public float StatTo { get; }
    public string Suffix { get; }
    public bool HasStatFrom { get; }
}
