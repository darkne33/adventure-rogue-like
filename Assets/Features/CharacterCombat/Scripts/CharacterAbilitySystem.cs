using System.Collections.Generic;
using UnityEngine;

public class CharacterAbilitySystem
{
    private readonly List<CharacterAbility> _abilities = new();
    
    private readonly Dictionary<AbilityName, CharacterActiveAbility> _activeAbilities = new();
    private readonly Dictionary<AbilityName, CharacterPassiveAbility> _passiveAbilities = new();

    public void AddAbility(CharacterAbility ability, CharacterFacade owner)
    {
        _abilities.Add(ability);
        ability.OnEquip(owner);

        if (ability is CharacterActiveAbility active)
            _activeAbilities[active.Id] = active;
        if (ability is CharacterPassiveAbility passive)
            _passiveAbilities[passive.Id] = passive;
    }

    public void RemoveAbility(AbilityName abilityId, CharacterFacade owner)
    {
        var ability = _abilities.Find(a => a.Id == abilityId);
        if (ability != null)
        {
            ability.OnUnequip(owner);
            _abilities.Remove(ability);

            if (ability is CharacterActiveAbility active)
                _activeAbilities.Remove(abilityId);
            
            if (ability is CharacterPassiveAbility passiveAbility)
                _passiveAbilities.Remove(abilityId);
        }
    }

    public void TickAbilities(CharacterFacade character)
    {
        foreach (var ability in _activeAbilities.Values)
        {
            ability.CurrentCooldown -= Time.deltaTime;
            ability.Use(character);
        }
    }
}