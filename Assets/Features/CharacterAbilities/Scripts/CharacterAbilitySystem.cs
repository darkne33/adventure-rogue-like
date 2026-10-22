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

    public float CalculateEstimatedDps()
    {
        float dps = 0f;
        foreach (CharacterActiveAbility ability in _activeAbilities.Values)
            dps += Mathf.Max(0f, ability.CalculateEstimatedDps());
        return dps;
    }

    public void TickAbilities(CharacterFacade character)
    {
        foreach (CharacterActiveAbility ability in _activeAbilities.Values)
        {
            ability.CurrentCooldown -= Time.deltaTime;
            ability.Use(character);
        }
    }

    public void RestartActiveCooldowns()
    {
        foreach (CharacterActiveAbility ability in _activeAbilities.Values)
            ability.RestartCooldown();
    }

    public void ApplyThornsKnockback(CharacterFacade character, CombatTarget target)
    {
        if (_passiveAbilities.TryGetValue(AbilityName.ThornsDamageScroll, out CharacterPassiveAbility ability))
            ability.ApplyHitKnockback(character, target);
    }

    public bool TryBlockIncomingDamage(CharacterFacade character, CombatTarget source, Vector3? origin = null)
    {
        foreach (CharacterActiveAbility ability in _activeAbilities.Values)
            if (ability is AegisAbility aegis && aegis.TryBlockAttack(character, source, origin))
                return true;
        return false;
    }

    public bool TryBlockProjectile(CharacterFacade character, CombatTarget source, Vector3 from,
        Vector3 to, float radius, out float hitDistance)
    {
        hitDistance = 0f;
        return _activeAbilities.TryGetValue(AbilityName.Aegis, out CharacterActiveAbility ability) &&
               ability is AegisAbility aegis &&
               aegis.TryBlockSegment(character, source, from, to, radius, out hitDistance);
    }

    public bool TryBlockBox(CharacterFacade character, CombatTarget source, Vector3 center,
        Vector3 halfExtents, Quaternion rotation) =>
        _activeAbilities.TryGetValue(AbilityName.Aegis, out CharacterActiveAbility ability) &&
        ability is AegisAbility aegis && aegis.TryBlockBox(character, source, center, halfExtents, rotation);

    public void Dispose()
    {
        foreach (CharacterActiveAbility ability in _activeAbilities.Values)
            if (ability is System.IDisposable disposable)
                disposable.Dispose();
    }

    public void ReduceActiveCooldowns(float seconds)
    {
        float reduction = Mathf.Max(0f, seconds);
        if (reduction <= 0f)
            return;

        foreach (CharacterActiveAbility ability in _activeAbilities.Values)
            ability.CurrentCooldown = Mathf.Max(0f, ability.CurrentCooldown - reduction);
    }
}
