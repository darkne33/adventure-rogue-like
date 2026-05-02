using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using UI;
using UnityEngine;

public class UpgradeOfferHandler : IUpgradeOfferHandler
{
    private readonly IUpgradeOfferGenerator _upgradeOfferGenerator;
    private readonly IUpgradeOfferItemFactory _upgradeOfferItemFactory;
    private readonly IPanelService _panelService;
    private readonly CharacterStats _characterStats;
    private readonly ICharacterProvider _characterProvider;

    private readonly List<UpgradeOfferItemView> _upgradeItems = new();

    public UpgradeOfferHandler(IUpgradeOfferGenerator upgradeOfferGenerator,
        IUpgradeOfferItemFactory upgradeOfferItemFactory, IPanelService panelService, CharacterStats characterStats, ICharacterProvider characterProvider)
    {
        _upgradeOfferGenerator = upgradeOfferGenerator;
        _upgradeOfferItemFactory = upgradeOfferItemFactory;
        _panelService = panelService;
        _characterStats = characterStats;
        _characterProvider = characterProvider;
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
            var upgradeItemOfferFacade = _upgradeOfferItemFactory.Create(upgradesRoot);
            UpgradeOfferItemView offerItemView = upgradeItemOfferFacade.UpgradeOfferItemView;

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
            _upgradeItems.Add(offerItemView);
            upgradeItemOfferFacade.UpgradeOfferItemApplyHandler.Initialize(ability);
        }
        
        upgradeOfferPanel.Show();
    }

    public void ApplyAbilityToCharacter(CharacterAbility characterAbility)
    {
        _characterProvider.CharacterFacade.CharacterAbilitySystem.AddAbility(characterAbility, _characterStats);
        var characterPanelPresenter =
            _panelService.GetPanelPresenter<CharacterPanelPresenter>(PanelName.CharacterPanel);

        var characterPanel = characterPanelPresenter.Panel;
        var upgradeOfferPanel = characterPanel.UpgradeOfferPanel;

        foreach (var upgradeItem in _upgradeItems) 
            Object.Destroy(upgradeItem.gameObject);

        upgradeOfferPanel.Hide().Forget();
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