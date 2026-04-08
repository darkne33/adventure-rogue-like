public abstract class CharacterPassiveAbility : CharacterAbility
{
    public abstract float GetIncreaseDefault(CharacterStats characterStats);
    public abstract float GetStatsToIncrease(CharacterStats characterStats);
}