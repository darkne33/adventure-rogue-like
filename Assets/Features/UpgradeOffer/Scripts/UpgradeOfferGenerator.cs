using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UpgradeOfferGenerator : IUpgradeOfferGenerator
{
    private const int TotalOfferCount = 3;

    private readonly IAbilityChoiceProvider _abilityChoiceProvider;
    private readonly UpgradeOfferConfiguration _upgradeOfferConfiguration;
    private readonly UpgradeBuildService _upgradeBuildService;

    public UpgradeOfferGenerator(IAbilityChoiceProvider abilityChoiceProvider,
        UpgradeOfferConfiguration upgradeOfferConfiguration, UpgradeBuildService upgradeBuildService)
    {
        _abilityChoiceProvider = abilityChoiceProvider;
        _upgradeOfferConfiguration = upgradeOfferConfiguration;
        _upgradeBuildService = upgradeBuildService;
    }

    public IEnumerable<UpgradeOffer> GenerateOffers()
    {
        List<UpgradeOffer> offers = new();
        List<CharacterAbility> offerAbilities = new();

        List<CharacterAbility> availableAbilities = _abilityChoiceProvider.GetCharacterAbilities().Values
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

        bool includeNewActiveAbility = ShouldIncludeNewActiveAbility(newActiveAbilities.Count > 0);
        bool includeSelectedActiveAbility = includeNewActiveAbility == false &&
                                            selectedActiveAbilities.Count > 0 &&
                                            RollChance(_upgradeOfferConfiguration.ActiveAbilityOfferChance);
        IEnumerable<CharacterActiveAbility> activeOfferAbilities = includeNewActiveAbility
            ? newActiveAbilities
            : selectedActiveAbilities;

        int countActiveAbilities = includeNewActiveAbility || includeSelectedActiveAbility ? 1 : 0;
        int countScrolls = TotalOfferCount - countActiveAbilities;

        AddRandomOffers(passiveAbilities, countScrolls, offerAbilities, offers);
        AddRandomOffers(activeOfferAbilities, countActiveAbilities, offerAbilities, offers);

        List<CharacterAbility> fallbackAbilities = availableAbilities
            .Where(ability => ability is not CharacterActiveAbility ||
                              _upgradeBuildService.Contains(ability))
            .ToList();
        AddRandomOffers(fallbackAbilities, TotalOfferCount - offers.Count, offerAbilities, offers);

        return offers;
    }

    private bool IsAvailableForCurrentBuild(CharacterAbility ability) =>
        _upgradeBuildService.CanSelect(ability);

    private bool ShouldIncludeNewActiveAbility(bool hasAvailableAbility)
    {
        if (hasAvailableAbility == false || _upgradeBuildService.IsFull)
            return false;

        float chance = _upgradeOfferConfiguration.GetNewActiveAbilityOfferChance(
            _upgradeBuildService.ActiveAbilityCount);
        return RollChance(chance);
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

        UpgradeRarityData rarityData = _upgradeOfferConfiguration.GetRandomRarityData();
        AbilityUpgradeType upgradeType = ability.GetRandomUpgradeType();
        float upgradeValue = upgradeType == AbilityUpgradeType.AdditionalProjectiles
            ? _upgradeOfferConfiguration.GetRandomProjectileCountIncrease(rarityData)
            : rarityData.UpgradeMultiplier;

        return new UpgradeOffer(ability, rarityData.Rarity, upgradeValue, upgradeType);
    }

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
    public IEnumerable<UpgradeOffer> GenerateOffers();
}
