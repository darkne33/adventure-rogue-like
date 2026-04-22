public abstract class CharacterPassiveAbility : CharacterAbility
{
    public abstract float GetStatFromIncrease(CharacterStats characterStats);
    public abstract float GetStatToIncrease(CharacterStats characterStats);
}