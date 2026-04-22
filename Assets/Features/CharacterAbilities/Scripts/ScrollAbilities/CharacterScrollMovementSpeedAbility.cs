public class CharacterScrollMovementSpeedAbility : CharacterPassiveAbility
{
    private ScrollAbilityConfiguration _scrollAbilityConfiguration;

    public override void OnUnequip(CharacterStats characterStats)
    {
        characterStats.MovementSpeed -= _scrollAbilityConfiguration.DefaultIncreaseStat;
    }

    public override void Initialize(AbilityConfiguration abilityConfig)
    {
        base.Initialize(abilityConfig);
        
        _scrollAbilityConfiguration = (ScrollAbilityConfiguration)abilityConfig;
    }

    public override void OnEquip(CharacterStats characterStats)
    {
        characterStats.MovementSpeed += _scrollAbilityConfiguration.DefaultIncreaseStat;
    }

    public override float GetStatFromIncrease(CharacterStats characterStats) => 
        characterStats.MovementSpeed;

    public override float GetStatToIncrease(CharacterStats characterStats) => 
        GetStatFromIncrease(characterStats) + _scrollAbilityConfiguration.DefaultIncreaseStat;
}