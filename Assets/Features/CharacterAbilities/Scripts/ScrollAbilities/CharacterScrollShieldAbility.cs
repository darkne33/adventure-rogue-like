using UnityEngine;

public class CharacterScrollShieldAbility : CharacterPassiveAbility
{
    private ScrollAbilityConfiguration _scrollAbilityConfiguration;

    public override void OnUnequip(CharacterStats characterStats)
    {
        base.OnUnequip(characterStats);
        characterStats.Shield = Mathf.Max(0f,
            characterStats.Shield - GetShieldIncrease(CurrentUpgradeMultiplier));
    }

    public override void Initialize(AbilityConfiguration abilityConfig)
    {
        base.Initialize(abilityConfig);
        _scrollAbilityConfiguration = (ScrollAbilityConfiguration)abilityConfig;
    }

    public override void OnEquip(CharacterStats characterStats)
    {
        base.OnEquip(characterStats);
        characterStats.Shield += GetShieldIncrease(CurrentUpgradeMultiplier);
    }

    public override float GetStatFromIncrease(CharacterStats characterStats) =>
        characterStats.Shield;

    public override float GetStatToIncrease(CharacterStats characterStats, float upgradeMultiplier) =>
        GetStatFromIncrease(characterStats) + GetShieldIncrease(upgradeMultiplier);

    private int GetShieldIncrease(float upgradeMultiplier) =>
        Mathf.Max(0, Mathf.RoundToInt(
            GetUpgradeValue(_scrollAbilityConfiguration.DefaultIncreaseStat, upgradeMultiplier)));
}
