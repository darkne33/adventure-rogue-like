public class CharacterScrollMovementSpeedAbility : CharacterPassiveAbility
{
    private ScrollAbilityConfiguration _scrollAbilityConfiguration;

    public override void OnUnequip(CharacterStats characterStats)
    {
        base.OnUnequip(characterStats);
        characterStats.MovementSpeed -= GetCurrentUpgradeValue(_scrollAbilityConfiguration.DefaultIncreaseStat);
    }

    public override void Initialize(AbilityConfiguration abilityConfig)
    {
        base.Initialize(abilityConfig);
        
        _scrollAbilityConfiguration = (ScrollAbilityConfiguration)abilityConfig;
    }

    public override void OnEquip(CharacterStats characterStats)
    {
        base.OnEquip(characterStats);
        characterStats.MovementSpeed += GetCurrentUpgradeValue(_scrollAbilityConfiguration.DefaultIncreaseStat);
    }

    public override float GetStatFromIncrease(CharacterStats characterStats) => 
        characterStats.MovementSpeed;

    public override float GetStatToIncrease(CharacterStats characterStats, float upgradeMultiplier) => 
        GetStatFromIncrease(characterStats) +
        GetUpgradeValue(_scrollAbilityConfiguration.DefaultIncreaseStat, upgradeMultiplier);
}
