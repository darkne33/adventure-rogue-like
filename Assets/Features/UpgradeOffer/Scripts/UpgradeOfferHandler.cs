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
    private readonly IPauseService _pauseService;

    private readonly List<UpgradeOfferItemView> _upgradeItems = new();

    private Transform UpgradesRoot => _panelService.GetPanelPresenter<CharacterPanelPresenter>(PanelName.CharacterPanel)
        .Panel.UpgradeOfferPanel.UpgradesRoot;
    private UpgradeOfferPanel UpgradeOfferPanel => _panelService.GetPanelPresenter<CharacterPanelPresenter>(PanelName.CharacterPanel)
        .Panel.UpgradeOfferPanel;

    public UpgradeOfferHandler(IUpgradeOfferGenerator upgradeOfferGenerator,
        IUpgradeOfferItemFactory upgradeOfferItemFactory, IPanelService panelService, CharacterStats characterStats,
        ICharacterProvider characterProvider, IPauseService pauseService)
    {
        _upgradeOfferGenerator = upgradeOfferGenerator;
        _upgradeOfferItemFactory = upgradeOfferItemFactory;
        _panelService = panelService;
        _characterStats = characterStats;
        _characterProvider = characterProvider;
        _pauseService = pauseService;
    }

    public void Handle()
    {
        GenerateUpgrades(UpgradesRoot);
        
        _pauseService.HandlePause();
        UpgradeOfferPanel.Show();
    }

    public void RefreshItems()
    {
        DestroyViews();
        
        GenerateUpgrades(UpgradesRoot);
    }

    public void SkipUpgrades()
    {
        DestroyViews();

        _pauseService.CancelPause();
        UpgradeOfferPanel.Hide().Forget();
    }

    public void ApplyAbilityToCharacter(CharacterAbility characterAbility)
    {
        _characterProvider.CharacterFacade.CharacterAbilitySystem.AddAbility(characterAbility, _characterStats);

        SkipUpgrades();
    }

    private void DestroyViews()
    {
        foreach (var upgradeItem in _upgradeItems)
            Object.Destroy(upgradeItem.gameObject);
        
        _upgradeItems.Clear();
    }

    private void GenerateUpgrades(Transform upgradesRoot)
    {
        var abilities = _upgradeOfferGenerator.GenerateOfferAbilities();

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