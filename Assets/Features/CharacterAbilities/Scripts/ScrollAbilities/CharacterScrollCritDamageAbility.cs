public class CharacterScrollCritDamageAbility : CharacterPassiveAbility
{
    private ScrollAbilityConfiguration _scrollAbilityConfiguration;

    public override void OnUnequip(CharacterStats characterStats)
    {
        base.OnUnequip(characterStats);
        characterStats.CritDamage -= _scrollAbilityConfiguration.DefaultIncreaseStat;
    }

    public override void Initialize(AbilityConfiguration abilityConfig)
    {
        base.Initialize(abilityConfig);
        
        _scrollAbilityConfiguration = (ScrollAbilityConfiguration)abilityConfig;
    }

    public override void OnEquip(CharacterStats characterStats)
    {
        base.OnEquip(characterStats);
        characterStats.CritDamage += _scrollAbilityConfiguration.DefaultIncreaseStat;
    }

    public override float GetStatFromIncrease(CharacterStats characterStats) => 
        characterStats.CritDamage;

    public override float GetStatToIncrease(CharacterStats characterStats) => 
        GetStatFromIncrease(characterStats) + _scrollAbilityConfiguration.DefaultIncreaseStat;
}