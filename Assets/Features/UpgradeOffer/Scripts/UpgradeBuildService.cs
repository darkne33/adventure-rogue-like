using System;
using System.Collections.Generic;

public sealed class UpgradeBuildService
{
    private readonly List<UpgradeBuildEntry> _selectedUpgrades = new();
    private readonly Dictionary<UpgradeOfferKey, int> _rejectedOfferCounts = new();
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

    public void RecordRejectedOffer(UpgradeOffer offer)
    {
        if (offer.HasRarity == false || offer.Ability == null)
            return;

        UpgradeOfferKey key = new(offer.Ability.Id, offer.UpgradeType);
        int rejectedOfferCount = _rejectedOfferCounts.TryGetValue(key, out int count)
            ? count
            : 0;
        _rejectedOfferCounts[key] = rejectedOfferCount + 1;
    }

    public void RecordSelectedOffer(UpgradeOffer offer)
    {
        if (offer.HasRarity == false || offer.Ability == null)
            return;

        UpgradeOfferKey key = new(offer.Ability.Id, offer.UpgradeType);
        _rejectedOfferCounts.Remove(key);
    }

    public int GetRejectedOfferCount(CharacterAbility ability, AbilityUpgradeType upgradeType)
    {
        if (ability == null)
            return 0;

        UpgradeOfferKey key = new(ability.Id, upgradeType);
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
        public UpgradeOfferKey(AbilityName abilityId, AbilityUpgradeType upgradeType)
        {
            AbilityId = abilityId;
            UpgradeType = upgradeType;
        }

        private AbilityName AbilityId { get; }
        private AbilityUpgradeType UpgradeType { get; }

        public bool Equals(UpgradeOfferKey other) =>
            AbilityId == other.AbilityId && UpgradeType == other.UpgradeType;

        public override bool Equals(object obj) =>
            obj is UpgradeOfferKey other && Equals(other);

        public override int GetHashCode() =>
            ((int)AbilityId * 397) ^ (int)UpgradeType;
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
