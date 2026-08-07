using System.Collections.Generic;
using UnityEngine;

public class CharacterAbilitySystem
{
    private readonly List<CharacterAbility> _abilities = new();
    
    private readonly Dictionary<AbilityName, CharacterActiveAbility> _activeAbilities = new();
    private readonly Dictionary<AbilityName, CharacterPassiveAbility> _passiveAbilities = new();

    public void AddAbility(CharacterAbility ability, CharacterStats characterStats) =>
        AddAbility(ability, characterStats, 1f, AbilityUpgradeType.Default);

    public void AddAbility(CharacterAbility ability, CharacterStats characterStats, float upgradeMultiplier,
        AbilityUpgradeType upgradeType = AbilityUpgradeType.Default)
        => AddAbility(ability, characterStats,
            new AbilityUpgradeEffect(upgradeType, upgradeMultiplier), null);

    public void AddAbility(CharacterAbility ability, CharacterStats characterStats,
        AbilityUpgradeEffect primaryUpgrade, AbilityUpgradeEffect? secondaryUpgrade)
    {
        if (_abilities.Contains(ability) == false)
            _abilities.Add(ability);
        
        ability.ApplyUpgrade(characterStats, primaryUpgrade, secondaryUpgrade);

        if (ability is CharacterActiveAbility active)
            _activeAbilities[active.Id] = active;
        if (ability is CharacterPassiveAbility passive)
            _passiveAbilities[passive.Id] = passive;
    }

    public void RemoveAbility(AbilityName abilityId, CharacterStats characterStats)
    {
        var ability = _abilities.Find(a => a.Id == abilityId);
        if (ability != null)
        {
            ability.OnUnequip(characterStats);
            _abilities.Remove(ability);

            if (ability is CharacterActiveAbility active)
                _activeAbilities.Remove(abilityId);
            
            if (ability is CharacterPassiveAbility passiveAbility)
                _passiveAbilities.Remove(abilityId);
        }
    }

    public void TickAbilities(CharacterFacade character)
    {
        foreach (CharacterActiveAbility ability in _activeAbilities.Values)
        {
            ability.CurrentCooldown -= Time.deltaTime;
            ability.Use(character);
        }
    }
}
