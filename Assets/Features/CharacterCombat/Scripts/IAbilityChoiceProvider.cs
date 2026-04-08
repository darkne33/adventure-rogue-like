using System.Collections.Generic;

public interface IAbilityChoiceProvider
{
    public void CreateAllAbilities();
    public CharacterAbility GetAbility(AbilityName abilityName);
    public Dictionary<AbilityName, CharacterAbility> GetCharacterAbilities();
}