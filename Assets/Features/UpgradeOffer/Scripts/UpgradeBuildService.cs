using System;
using System.Collections.Generic;

public sealed class UpgradeBuildService
{
    private readonly List<UpgradeBuildEntry> _selectedUpgrades = new();
    private readonly int _maxSlots;
    private readonly int _maxActiveAbilities;

    public UpgradeBuildService(UpgradeOfferConfiguration configuration)
    {
        _maxSlots = Math.Max(1, configuration.MaxBuildSlots);
        _maxActiveAbilities = Math.Max(1, configuration.MaxActiveAbilities);
    }

    public event Action Changed;

    public IReadOnlyList<UpgradeBuildEntry> SelectedUpgrades => _selectedUpgrades;
    public int MaxSlots => _maxSlots;
    public int MaxActiveAbilities => _maxActiveAbilities;
    public bool IsFull => _selectedUpgrades.Count >= _maxSlots;
    public int ActiveAbilityCount
    {
        get
        {
            int count = 0;
            foreach (UpgradeBuildEntry entry in _selectedUpgrades)
            {
                if (entry.Ability is CharacterActiveAbility)
                    count++;
            }

            return count;
        }
    }

    public bool Contains(CharacterAbility ability) =>
        FindEntry(ability) != null;

    public bool CanSelect(CharacterAbility ability)
    {
        if (ability == null)
            return false;
        if (Contains(ability))
            return true;
        if (IsFull)
            return false;

        return ability is not CharacterActiveAbility ||
               ActiveAbilityCount < _maxActiveAbilities;
    }

    public bool RecordAppliedSelection(CharacterAbility ability)
    {
        if (CanSelect(ability) == false)
            return false;

        UpgradeBuildEntry entry = FindEntry(ability);
        if (entry == null)
            _selectedUpgrades.Add(new UpgradeBuildEntry(ability));
        else
            entry.IncreaseLevel();

        Changed?.Invoke();
        return true;
    }

    public void Reset()
    {
        if (_selectedUpgrades.Count == 0)
            return;

        _selectedUpgrades.Clear();
        Changed?.Invoke();
    }

    private UpgradeBuildEntry FindEntry(CharacterAbility ability) =>
        ability == null
            ? null
            : _selectedUpgrades.Find(entry => entry.Ability.Id == ability.Id);
}

public sealed class UpgradeBuildEntry
{
    public UpgradeBuildEntry(CharacterAbility ability)
    {
        Ability = ability;
    }

    public CharacterAbility Ability { get; }
    public int Level { get; private set; } = 1;

    public void IncreaseLevel() =>
        Level++;
}
