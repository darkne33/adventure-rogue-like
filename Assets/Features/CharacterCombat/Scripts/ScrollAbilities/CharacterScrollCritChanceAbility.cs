public class CharacterScrollCritChanceAbility : CharacterPassiveAbility
{
    private ScrollAbilityConfiguration _scrollAbilityConfiguration;

    public override void OnUnequip(CharacterStats characterStats)
    {
        base.OnUnequip(characterStats);
        characterStats.CritChance -= _scrollAbilityConfiguration.DefaultIncreaseStat;
    }

    public override void Initialize(AbilityConfiguration abilityConfig)
    {
        base.Initialize(abilityConfig);
        
        _scrollAbilityConfiguration = (ScrollAbilityConfiguration)abilityConfig;
    }

    public override void OnEquip(CharacterStats characterStats)
    {
        base.OnEquip(characterStats);
        characterStats.CritChance += _scrollAbilityConfiguration.DefaultIncreaseStat;
    }

    public override float GetIncreaseDefault(CharacterStats characterStats) => 
        characterStats.CritChance;

    public override float GetStatsToIncrease(CharacterStats characterStats) => 
        GetIncreaseDefault(characterStats) + _scrollAbilityConfiguration.DefaultIncreaseStat;
}