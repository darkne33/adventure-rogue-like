using System.Collections.Generic;
using System.Linq;
using CustomPackages.Package.Extensions;

public class UpgradeOfferGenerator : IUpgradeOfferGenerator
{
    private readonly IAbilityChoiceProvider _abilityChoiceProvider;
    private readonly UpgradeOfferConfiguration _upgradeOfferConfiguration;

    public UpgradeOfferGenerator(IAbilityChoiceProvider abilityChoiceProvider,
        UpgradeOfferConfiguration upgradeOfferConfiguration)
    {
        _abilityChoiceProvider = abilityChoiceProvider;
        _upgradeOfferConfiguration = upgradeOfferConfiguration;
    }

    public IEnumerable<UpgradeOffer> GenerateOffers()
    {
        List<UpgradeOffer> offers = new();
        List<CharacterAbility> offerAbilities = new();

        int countScrolls = 2;
        int countActiveAbilities = 1;

        List<CharacterPassiveAbility> passiveAbilities = _abilityChoiceProvider.GetCharacterAbilities().Values
            .OfType<CharacterPassiveAbility>().ToList();

        List<CharacterActiveAbility> activeAbilities = _abilityChoiceProvider.GetCharacterAbilities().Values
            .OfType<CharacterActiveAbility>().ToList();

        for (int i = 0; i < countScrolls; i++)
        {
            var randomScroll = GetRandomAbilityWithoutRepeat(passiveAbilities, offerAbilities);
            offerAbilities.Add(randomScroll);
            offers.Add(CreateOffer(randomScroll));
        }

        for (int i = 0; i < countActiveAbilities; i++)
        {
            var randomActiveAbility = GetRandomAbilityWithoutRepeat(activeAbilities, offerAbilities);
            offerAbilities.Add(randomActiveAbility);
            offers.Add(CreateOffer(randomActiveAbility));
        }

        return offers;
    }

    private UpgradeOffer CreateOffer(CharacterAbility ability)
    {
        if (ability.IsAcquired == false)
            return UpgradeOffer.CreateNew(ability);

        UpgradeRarityData rarityData = _upgradeOfferConfiguration.GetRandomRarityData();
        return new UpgradeOffer(ability, rarityData.Rarity, rarityData.UpgradeMultiplier,
            ability.GetRandomUpgradeType());
    }

    private T GetRandomAbilityWithoutRepeat<T>(List<T> allAbilities, List<CharacterAbility> offerAbilities)
        where T : CharacterAbility
    {
        T randomAbility;

        do
        {
            randomAbility = allAbilities.GetRandom();
        } while (offerAbilities.Contains(randomAbility) && allAbilities.Count > 1);

        return randomAbility;
    }
}

public interface IUpgradeOfferGenerator
{
    public IEnumerable<UpgradeOffer> GenerateOffers();
}
