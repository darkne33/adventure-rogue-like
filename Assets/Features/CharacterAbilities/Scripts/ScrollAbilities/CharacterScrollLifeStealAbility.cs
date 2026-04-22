public class CharacterScrollLifeStealAbility : CharacterPassiveAbility
{
    private ScrollAbilityConfiguration _scrollAbilityConfiguration;

    public override void OnUnequip(CharacterStats characterStats)
    {
        base.OnUnequip(characterStats);
        characterStats.LifeSteal -= _scrollAbilityConfiguration.DefaultIncreaseStat;
    }

    public override void Initialize(AbilityConfiguration abilityConfig)
    {
        base.Initialize(abilityConfig);
        
        _scrollAbilityConfiguration = (ScrollAbilityConfiguration)abilityConfig;
    }

    public override void OnEquip(CharacterStats characterStats)
    {
        base.OnEquip(characterStats);
        characterStats.LifeSteal += _scrollAbilityConfiguration.DefaultIncreaseStat;
    }

    public override float GetStatFromIncrease(CharacterStats characterStats) => 
        characterStats.LifeSteal;

    public override float GetStatToIncrease(CharacterStats characterStats) => 
        GetStatFromIncrease(characterStats) + _scrollAbilityConfiguration.DefaultIncreaseStat;
}