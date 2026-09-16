using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UpgradeOfferGenerator : IUpgradeOfferGenerator
{
    private const int TotalOfferCount = 3;
    private const int LevelsPerGuaranteedPassiveAbility = 5;
    private const int MaxGuaranteedPassiveAbilityCount = 2;

    private readonly IAbilityChoiceProvider _abilityChoiceProvider;
    private readonly ICharacterLevelService _characterLevelService;
    private readonly UpgradeOfferConfiguration _upgradeOfferConfiguration;
    private readonly UpgradeBuildService _upgradeBuildService;
    private readonly CharacterConfiguration _characterConfiguration;

    public UpgradeOfferGenerator(IAbilityChoiceProvider abilityChoiceProvider,
        ICharacterLevelService characterLevelService, UpgradeOfferConfiguration upgradeOfferConfiguration,
        UpgradeBuildService upgradeBuildService, CharacterConfiguration characterConfiguration)
    {
        _abilityChoiceProvider = abilityChoiceProvider;
        _characterLevelService = characterLevelService;
        _upgradeOfferConfiguration = upgradeOfferConfiguration;
        _upgradeBuildService = upgradeBuildService;
        _characterConfiguration = characterConfiguration;
    }

    public IEnumerable<UpgradeOffer> GenerateOffers(bool isNewUpgrade = true)
    {
        if (isNewUpgrade)
            _upgradeBuildService.BeginUpgradeRound();

        List<UpgradeOffer> offers = new();
        List<CharacterAbility> offerAbilities = new();

        List<CharacterAbility> availableAbilities = _abilityChoiceProvider.GetCharacterAbilities().Values
            .Where(ability => _characterConfiguration.SelectedCharacter.IsAvailableInUpgrades(ability.Id))
            .Where(IsAvailableForCurrentBuild)
            .ToList();

        List<CharacterPassiveAbility> passiveAbilities = availableAbilities
            .OfType<CharacterPassiveAbility>().ToList();

        List<CharacterActiveAbility> selectedActiveAbilities = availableAbilities
            .OfType<CharacterActiveAbility>()
            .Where(_upgradeBuildService.Contains)
            .ToList();

        List<CharacterActiveAbility> newActiveAbilities = availableAbilities
            .OfType<CharacterActiveAbility>()
            .Where(ability => _upgradeBuildService.Contains(ability) == false)
            .ToList();

        AddRandomOffers(newActiveAbilities, 1, offerAbilities, offers);
        AddRandomOffers(selectedActiveAbilities, 1, offerAbilities, offers);
        AddPassiveOffer(passiveAbilities, offerAbilities, offers);

        if (newActiveAbilities.Count == 0 && offers.Count < TotalOfferCount)
        {
            if (RollChance(_upgradeOfferConfiguration.ActiveAbilityOfferChance))
                AddRandomOffers(selectedActiveAbilities, 1, offerAbilities, offers);
            else
                AddRandomOffers(passiveAbilities, 1, offerAbilities, offers);
        }

        AddRandomOffers(availableAbilities, TotalOfferCount - offers.Count, offerAbilities, offers);

        return offers;
    }

    private bool IsAvailableForCurrentBuild(CharacterAbility ability) =>
        _upgradeBuildService.CanOffer(ability);

    private void AddPassiveOffer(List<CharacterPassiveAbility> passiveAbilities,
        List<CharacterAbility> offerAbilities, List<UpgradeOffer> offers)
    {
        if (NeedsGuaranteedPassiveAbility())
        {
            List<CharacterPassiveAbility> newPassiveAbilities = passiveAbilities
                .Where(ability => _upgradeBuildService.Contains(ability) == false)
                .ToList();

            if (newPassiveAbilities.Count > 0)
            {
                AddRandomOffers(newPassiveAbilities, 1, offerAbilities, offers);
                return;
            }
        }

        AddRandomOffers(passiveAbilities, 1, offerAbilities, offers);
    }

    private bool NeedsGuaranteedPassiveAbility()
    {
        int requiredPassiveAbilityCount = Mathf.Min(
            _characterLevelService.GetLevel / LevelsPerGuaranteedPassiveAbility,
            MaxGuaranteedPassiveAbilityCount);

        return _upgradeBuildService.IsFull == false &&
               _upgradeBuildService.PassiveAbilityCount < requiredPassiveAbilityCount;
    }

    private static bool RollChance(float chance)
    {
        if (chance <= 0f)
            return false;
        if (chance >= 100f)
            return true;

        return Random.Range(0f, 100f) < chance;
    }

    private UpgradeOffer CreateOffer(CharacterAbility ability)
    {
        if (ability.IsAcquired == false)
            return UpgradeOffer.CreateNew(ability);

        return ability is CharacterActiveAbility activeAbility
            ? CreateActiveAbilityOffer(activeAbility)
            : CreateSingleUpgradeOffer(ability);
    }

    private UpgradeOffer CreateActiveAbilityOffer(CharacterActiveAbility ability)
    {
        (AbilityUpgradeType primaryType, AbilityUpgradeType secondaryType) = GetRandomUpgradePair(ability);
        int rejectedOfferCount =
            _upgradeBuildService.GetRejectedOfferCount(ability, primaryType, secondaryType);
        UpgradeRarityData rarityData =
            _upgradeOfferConfiguration.GetRandomRarityData(rejectedOfferCount);

        return new UpgradeOffer(ability, rarityData.Rarity,
            CreateUpgradeEffect(primaryType, rarityData),
            CreateUpgradeEffect(secondaryType, rarityData));
    }

    private UpgradeOffer CreateSingleUpgradeOffer(CharacterAbility ability)
    {
        AbilityUpgradeType upgradeType = ability.GetRandomUpgradeType();
        int rejectedOfferCount = _upgradeBuildService.GetRejectedOfferCount(ability, upgradeType,
            AbilityUpgradeType.Default);
        UpgradeRarityData rarityData =
            _upgradeOfferConfiguration.GetRandomRarityData(rejectedOfferCount);

        return new UpgradeOffer(ability, rarityData.Rarity,
            CreateUpgradeEffect(upgradeType, rarityData));
    }

    private AbilityUpgradeEffect CreateUpgradeEffect(AbilityUpgradeType upgradeType,
        UpgradeRarityData rarityData)
    {
        float value = upgradeType == AbilityUpgradeType.AdditionalProjectiles
            ? _upgradeOfferConfiguration.GetRandomProjectileCountIncrease(rarityData)
            : rarityData.UpgradeMultiplier;

        return new AbilityUpgradeEffect(upgradeType, value);
    }

    private (AbilityUpgradeType Primary, AbilityUpgradeType Secondary) GetRandomUpgradePair(
        CharacterActiveAbility ability)
    {
        List<AbilityUpgradeType> upgradeTypes = ability.UpgradeTypes
            .Where(type => type != AbilityUpgradeType.Default)
            .Distinct()
            .ToList();
        if (upgradeTypes.Count < 2)
            throw new System.InvalidOperationException(
                $"{ability.GetType().Name} must provide at least two different upgrade types.");

        bool canIncreaseProjectileCount = upgradeTypes.Remove(AbilityUpgradeType.AdditionalProjectiles);
        bool includeAdditionalProjectiles = canIncreaseProjectileCount &&
                                            (upgradeTypes.Count < 2 ||
                                             RollChance(_upgradeOfferConfiguration
                                                 .AdditionalProjectilesUpgradeOfferChance));

        if (includeAdditionalProjectiles)
        {
            AbilityUpgradeType pairedType = upgradeTypes[Random.Range(0, upgradeTypes.Count)];
            return OrderPair(AbilityUpgradeType.AdditionalProjectiles, pairedType);
        }

        int primaryIndex = Random.Range(0, upgradeTypes.Count);
        AbilityUpgradeType primaryType = upgradeTypes[primaryIndex];
        upgradeTypes.RemoveAt(primaryIndex);
        AbilityUpgradeType secondaryType = upgradeTypes[Random.Range(0, upgradeTypes.Count)];
        return OrderPair(primaryType, secondaryType);
    }

    private static (AbilityUpgradeType Primary, AbilityUpgradeType Secondary) OrderPair(
        AbilityUpgradeType first, AbilityUpgradeType second) =>
        (int)first <= (int)second ? (first, second) : (second, first);

    private void AddRandomOffers<T>(IEnumerable<T> source, int requestedCount,
        List<CharacterAbility> offerAbilities, List<UpgradeOffer> offers)
        where T : CharacterAbility
    {
        List<T> available = source
            .Where(ability => offerAbilities.Contains(ability) == false)
            .ToList();
        int count = Mathf.Min(requestedCount, available.Count);

        for (int index = 0; index < count; index++)
        {
            int randomIndex = Random.Range(0, available.Count);
            T ability = available[randomIndex];
            available.RemoveAt(randomIndex);

            offerAbilities.Add(ability);
            offers.Add(CreateOffer(ability));
        }
    }
}

public interface IUpgradeOfferGenerator
{
    public IEnumerable<UpgradeOffer> GenerateOffers(bool isNewUpgrade = true);
}
