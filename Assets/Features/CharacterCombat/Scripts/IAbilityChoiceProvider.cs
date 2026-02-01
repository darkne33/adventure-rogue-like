public interface IAbilityChoiceProvider
{
    void CreateAllAbilities();
    CharacterAbility GetAbility(AbilityName abilityName);
}