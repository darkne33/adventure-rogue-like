public class CharacterScrollArmorAbility : CharacterPassiveAbility
{
    private ScrollAbilityConfiguration _scrollAbilityConfiguration;

    public override void OnUnequip(CharacterStats characterStats)
    {
        characterStats.Armor -= _scrollAbilityConfiguration.DefaultIncreaseStat;
    }

    public override void Initialize(AbilityConfiguration abilityConfig)
    {
        base.Initialize(abilityConfig);
        
        _scrollAbilityConfiguration = (ScrollAbilityConfiguration)abilityConfig;
    }

    public override void OnEquip(CharacterStats characterStats)
    {
        characterStats.Armor += _scrollAbilityConfiguration.DefaultIncreaseStat;
    }

    public override float GetIncreaseDefault(CharacterStats characterStats) => 
        characterStats.Armor;

    public override float GetStatsToIncrease(CharacterStats characterStats) => 
        GetIncreaseDefault(characterStats) + _scrollAbilityConfiguration.DefaultIncreaseStat;
}