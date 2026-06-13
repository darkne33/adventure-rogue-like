public class CharacterScrollMaxHpAbility : CharacterPassiveAbility
{
    private ScrollAbilityConfiguration _scrollAbilityConfiguration;

    public override void OnUnequip(CharacterStats characterStats)
    {
        base.OnUnequip(characterStats);
        characterStats.MaxHp -= _scrollAbilityConfiguration.DefaultIncreaseStat;
    }

    public override void Initialize(AbilityConfiguration abilityConfig)
    {
        base.Initialize(abilityConfig);
        
        _scrollAbilityConfiguration = (ScrollAbilityConfiguration)abilityConfig;
    }

    public override void OnEquip(CharacterStats characterStats)
    {
        base.OnEquip(characterStats);
        characterStats.MaxHp += _scrollAbilityConfiguration.DefaultIncreaseStat;
    }

    public override float GetStatFromIncrease(CharacterStats characterStats) => 
        characterStats.MaxHp;

    public override float GetStatToIncrease(CharacterStats characterStats) => 
        GetStatFromIncrease(characterStats) + _scrollAbilityConfiguration.DefaultIncreaseStat;
}
