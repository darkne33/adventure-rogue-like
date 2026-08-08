using System;
using System.Collections.Generic;

public sealed class UpgradeBuildService
{
    private readonly List<UpgradeBuildEntry> _selectedUpgrades = new();
    private readonly Dictionary<UpgradeOfferKey, int> _rejectedOfferCounts = new();
    private readonly int _maxSlots;
    private readonly int _maxActiveAbilities;
    private readonly int _maxPassiveAbilities;

    public UpgradeBuildService(UpgradeOfferConfiguration configuration)
    {
        _maxSlots = Math.Max(1, configuration.MaxBuildSlots);
        _maxActiveAbilities = Math.Max(1, configuration.MaxActiveAbilities);
        _maxPassiveAbilities = Math.Max(1, configuration.MaxPassiveAbilities);
    }

    public event Action Changed;

    public IReadOnlyList<UpgradeBuildEntry> SelectedUpgrades => _selectedUpgrades;
    public int MaxSlots => _maxSlots;
    public int MaxActiveAbilities => _maxActiveAbilities;
    public int MaxPassiveAbilities => _maxPassiveAbilities;
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

    public int PassiveAbilityCount
    {
        get
        {
            int count = 0;
            foreach (UpgradeBuildEntry entry in _selectedUpgrades)
            {
                if (entry.Ability is CharacterPassiveAbility)
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

        return ability switch
        {
            CharacterActiveAbility => ActiveAbilityCount < _maxActiveAbilities,
            CharacterPassiveAbility => PassiveAbilityCount < _maxPassiveAbilities,
            _ => true
        };
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

    public void RecordRejectedOffer(UpgradeOffer offer)
    {
        if (offer.HasRarity == false || offer.Ability == null)
            return;

        UpgradeOfferKey key = new(offer.Ability.Id, offer.PrimaryUpgrade.Type,
            offer.SecondaryUpgrade?.Type ?? AbilityUpgradeType.Default);
        int rejectedOfferCount = _rejectedOfferCounts.TryGetValue(key, out int count)
            ? count
            : 0;
        _rejectedOfferCounts[key] = rejectedOfferCount + 1;
    }

    public void RecordSelectedOffer(UpgradeOffer offer)
    {
        if (offer.HasRarity == false || offer.Ability == null)
            return;

        UpgradeOfferKey key = new(offer.Ability.Id, offer.PrimaryUpgrade.Type,
            offer.SecondaryUpgrade?.Type ?? AbilityUpgradeType.Default);
        _rejectedOfferCounts.Remove(key);
    }

    public int GetRejectedOfferCount(CharacterAbility ability, AbilityUpgradeType primaryUpgradeType,
        AbilityUpgradeType secondaryUpgradeType)
    {
        if (ability == null)
            return 0;

        UpgradeOfferKey key = new(ability.Id, primaryUpgradeType, secondaryUpgradeType);
        return _rejectedOfferCounts.TryGetValue(key, out int count) ? count : 0;
    }

    public void Reset()
    {
        _rejectedOfferCounts.Clear();

        if (_selectedUpgrades.Count == 0)
            return;

        _selectedUpgrades.Clear();
        Changed?.Invoke();
    }

    private UpgradeBuildEntry FindEntry(CharacterAbility ability) =>
        ability == null
            ? null
            : _selectedUpgrades.Find(entry => entry.Ability.Id == ability.Id);

    private readonly struct UpgradeOfferKey : IEquatable<UpgradeOfferKey>
    {
        public UpgradeOfferKey(AbilityName abilityId, AbilityUpgradeType firstUpgradeType,
            AbilityUpgradeType secondUpgradeType)
        {
            AbilityId = abilityId;
            if ((int)firstUpgradeType <= (int)secondUpgradeType)
            {
                FirstUpgradeType = firstUpgradeType;
                SecondUpgradeType = secondUpgradeType;
            }
            else
            {
                FirstUpgradeType = secondUpgradeType;
                SecondUpgradeType = firstUpgradeType;
            }
        }

        private AbilityName AbilityId { get; }
        private AbilityUpgradeType FirstUpgradeType { get; }
        private AbilityUpgradeType SecondUpgradeType { get; }

        public bool Equals(UpgradeOfferKey other) =>
            AbilityId == other.AbilityId &&
            FirstUpgradeType == other.FirstUpgradeType &&
            SecondUpgradeType == other.SecondUpgradeType;

        public override bool Equals(object obj) =>
            obj is UpgradeOfferKey other && Equals(other);

        public override int GetHashCode() =>
            (((int)AbilityId * 397) ^ (int)FirstUpgradeType) * 397 ^ (int)SecondUpgradeType;
    }
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
