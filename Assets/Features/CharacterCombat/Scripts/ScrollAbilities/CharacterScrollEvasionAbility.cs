public class CharacterScrollEvasionAbility : CharacterPassiveAbility
{
    private ScrollAbilityConfiguration _scrollAbilityConfiguration;

    public override void OnUnequip(CharacterStats characterStats)
    {
        characterStats.Evasion -= _scrollAbilityConfiguration.DefaultIncreaseStat;
    }

    public override void Initialize(AbilityConfiguration abilityConfig)
    {
        base.Initialize(abilityConfig);
        
        _scrollAbilityConfiguration = (ScrollAbilityConfiguration)abilityConfig;
    }

    public override void OnEquip(CharacterStats characterStats)
    {
        characterStats.Evasion += _scrollAbilityConfiguration.DefaultIncreaseStat;
    }

    public override float GetIncreaseDefault(CharacterStats characterStats) => 
        characterStats.Evasion;

    public override float GetStatsToIncrease(CharacterStats characterStats) => 
        GetIncreaseDefault(characterStats) + _scrollAbilityConfiguration.DefaultIncreaseStat;
}