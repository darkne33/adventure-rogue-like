public class CharacterScrollEvasionAbility : CharacterPassiveAbility
{
    private ScrollAbilityConfiguration _scrollAbilityConfiguration;

    public override void OnUnequip(CharacterStats characterStats)
    {
        base.OnUnequip(characterStats);
        characterStats.Evasion -= GetCurrentUpgradeValue(_scrollAbilityConfiguration.DefaultIncreaseStat);
    }

    public override void Initialize(AbilityConfiguration abilityConfig)
    {
        base.Initialize(abilityConfig);
        
        _scrollAbilityConfiguration = (ScrollAbilityConfiguration)abilityConfig;
    }

    public override void OnEquip(CharacterStats characterStats)
    {
        base.OnEquip(characterStats);
        characterStats.Evasion += GetCurrentUpgradeValue(_scrollAbilityConfiguration.DefaultIncreaseStat);
    }

    public override float GetStatFromIncrease(CharacterStats characterStats) => 
        characterStats.Evasion;

    public override float GetStatToIncrease(CharacterStats characterStats, float upgradeMultiplier) => 
        GetStatFromIncrease(characterStats) +
        GetUpgradeValue(_scrollAbilityConfiguration.DefaultIncreaseStat, upgradeMultiplier);
}
