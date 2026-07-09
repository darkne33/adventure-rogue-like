public abstract class CharacterPassiveAbility : CharacterAbility
{
    public string StatSuffix { get; private set; } = string.Empty;

    public override void Initialize(AbilityConfiguration abilityConfig)
    {
        base.Initialize(abilityConfig);
        StatSuffix = abilityConfig is ScrollAbilityConfiguration scrollConfiguration
            ? scrollConfiguration.StatSuffix ?? string.Empty
            : string.Empty;
    }

    public abstract float GetStatFromIncrease(CharacterStats characterStats);

    public float GetStatToIncrease(CharacterStats characterStats) =>
        GetStatToIncrease(characterStats, 1f);

    public abstract float GetStatToIncrease(CharacterStats characterStats, float upgradeMultiplier);
}
