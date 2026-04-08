public class CharacterScrollRegenHpAbility : CharacterPassiveAbility
{
    private ScrollAbilityConfiguration _scrollAbilityConfiguration;

    public override void OnUnequip(CharacterStats characterStats)
    {
        characterStats.RegenHp -= _scrollAbilityConfiguration.DefaultIncreaseStat;
    }

    public override void Initialize(AbilityConfiguration abilityConfig)
    {
        base.Initialize(abilityConfig);
        
        _scrollAbilityConfiguration = (ScrollAbilityConfiguration)abilityConfig;
    }

    public override void OnEquip(CharacterStats characterStats)
    {
        characterStats.RegenHp += _scrollAbilityConfiguration.DefaultIncreaseStat;
    }

    public override float GetIncreaseDefault(CharacterStats characterStats) => 
        characterStats.RegenHp;

    public override float GetStatsToIncrease(CharacterStats characterStats) => 
        GetIncreaseDefault(characterStats) + _scrollAbilityConfiguration.DefaultIncreaseStat;
}