using System.Linq;
using System.Text.RegularExpressions;
using UI;

public class UpgradeOfferHandler : IUpgradeOfferHandler
{
    private readonly IUpgradeOfferGenerator _upgradeOfferGenerator;
    private readonly IUpgradeOfferItemFactory _upgradeOfferItemFactory;
    private readonly IPanelService _panelService;
    private readonly CharacterStats _characterStats;

    public UpgradeOfferHandler(IUpgradeOfferGenerator upgradeOfferGenerator,
        IUpgradeOfferItemFactory upgradeOfferItemFactory, IPanelService panelService, CharacterStats characterStats)
    {
        _upgradeOfferGenerator = upgradeOfferGenerator;
        _upgradeOfferItemFactory = upgradeOfferItemFactory;
        _panelService = panelService;
        _characterStats = characterStats;
    }

    public void Handle()
    {
        var abilities = _upgradeOfferGenerator.GenerateOfferAbilities();
        var characterPanelPresenter =
            _panelService.GetPanelPresenter<CharacterPanelPresenter>(PanelName.CharacterPanel);

        var characterPanel = characterPanelPresenter.Panel;
        var upgradeOfferPanel = characterPanel.UpgradeOfferPanel;
        var upgradesRoot = upgradeOfferPanel.UpgradesRoot;

        foreach (CharacterAbility ability in abilities)
        {
            UpgradeOfferItemView offerItemView = _upgradeOfferItemFactory.Create(upgradesRoot);

            offerItemView.DeactivateSkillsDescriptions();
            offerItemView.SetupName(ability.DisplayName);
            offerItemView.SetupIcon(ability.Icon);
            offerItemView.SetupLevel(ability.Level);

            switch (ability)
            {
                case CharacterPassiveAbility passiveAbility:
                    string statName = CleanCharacterStatName(passiveAbility.Id.ToString());
                    offerItemView.SetupSkillDescription_1(statName,
                        (int)passiveAbility.GetStatFromIncrease(_characterStats),
                        (int)passiveAbility.GetStatToIncrease(_characterStats));
                    break;
                case CharacterActiveAbility activeAbility:
                    offerItemView.SetupSkillDescription_1(activeAbility.StatName_1,
                        (int)activeAbility.GetStatFromIncrease(), (int)activeAbility.GetStatToIncrease());
                    break;
            }
        }
        
        upgradeOfferPanel.Show();
    }

    private string CleanCharacterStatName(string abilityName)
    {
        string wordToRemove = "Scroll";

        var words = Regex.Split(abilityName, @"(?<!^)(?=[A-Z])")
            .Where(w => !string.IsNullOrEmpty(w))
            .ToList();

        words.Remove(wordToRemove);

        string result = string.Join(" ", words);
        return result;
    }
}