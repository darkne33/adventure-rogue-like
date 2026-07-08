public abstract class CharacterPassiveAbility : CharacterAbility
{
    public abstract float GetStatFromIncrease(CharacterStats characterStats);

    public float GetStatToIncrease(CharacterStats characterStats) =>
        GetStatToIncrease(characterStats, 1f);

    public abstract float GetStatToIncrease(CharacterStats characterStats, float upgradeMultiplier);
}
