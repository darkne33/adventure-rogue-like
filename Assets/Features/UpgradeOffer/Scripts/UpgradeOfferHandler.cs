using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using UI;
using UnityEngine;

public class UpgradeOfferHandler : IUpgradeOfferHandler, IDisposable
{
    private readonly IUpgradeOfferGenerator _upgradeOfferGenerator;
    private readonly IUpgradeOfferItemFactory _upgradeOfferItemFactory;
    private readonly IPanelService _panelService;
    private readonly CharacterStats _characterStats;
    private readonly ICharacterProvider _characterProvider;
    private readonly IPauseService _pauseService;
    private readonly ICharacterLevelService _characterLevelService;

    private readonly List<UpgradeOfferItemView> _upgradeItems = new();

    private int _pendingOffers;
    private bool _isOpen;
    private bool _isClosing;

    private Transform UpgradesRoot => _panelService.GetPanelPresenter<CharacterPanelPresenter>(PanelName.CharacterPanel)
        .Panel.UpgradeOfferPanel.UpgradesRoot;

    private UpgradeOfferPanel UpgradeOfferPanel =>
        _panelService.GetPanelPresenter<CharacterPanelPresenter>(PanelName.CharacterPanel)
            .Panel.UpgradeOfferPanel;

    public UpgradeOfferHandler(IUpgradeOfferGenerator upgradeOfferGenerator,
        IUpgradeOfferItemFactory upgradeOfferItemFactory, IPanelService panelService, CharacterStats characterStats,
        ICharacterProvider characterProvider, IPauseService pauseService,
        ICharacterLevelService characterLevelService)
    {
        _upgradeOfferGenerator = upgradeOfferGenerator;
        _upgradeOfferItemFactory = upgradeOfferItemFactory;
        _panelService = panelService;
        _characterStats = characterStats;
        _characterProvider = characterProvider;
        _pauseService = pauseService;
        _characterLevelService = characterLevelService;

        _characterLevelService.OnLevelUp += OnLevelUp;
    }

    public void Handle()
    {
        _pendingOffers++;
        TryShowNextOffer();
    }

    public void RefreshItems()
    {
        if (_isOpen == false || _isClosing)
            return;

        DestroyViews();
        GenerateUpgrades(UpgradesRoot);
    }

    public void SkipUpgrades()
    {
        if (_isOpen == false || _isClosing)
            return;

        _isClosing = true;
        CloseCurrentOffer().Forget();
    }

    public void ApplyAbilityToCharacter(CharacterAbility characterAbility)
    {
        _characterProvider.CharacterFacade.CharacterAbilitySystem.AddAbility(characterAbility, _characterStats);
        SkipUpgrades();
    }

    public void Dispose()
    {
        _characterLevelService.OnLevelUp -= OnLevelUp;

        if (_isOpen)
            _pauseService.CancelPause();
    }

    private void OnLevelUp(int level) =>
        Handle();

    private void TryShowNextOffer()
    {
        if (_isOpen || _isClosing || _pendingOffers <= 0)
            return;

        _pendingOffers--;
        _isOpen = true;

        DestroyViews();
        GenerateUpgrades(UpgradesRoot);

        _pauseService.HandlePause();
        UpgradeOfferPanel.Show().Forget();
    }

    private async UniTask CloseCurrentOffer()
    {
        DestroyViews();
        await UpgradeOfferPanel.Hide();

        _pauseService.CancelPause();
        _isOpen = false;
        _isClosing = false;
        TryShowNextOffer();
    }

    private void DestroyViews()
    {
        foreach (UpgradeOfferItemView upgradeItem in _upgradeItems)
            UnityEngine.Object.Destroy(upgradeItem.gameObject);

        _upgradeItems.Clear();
    }

    private void GenerateUpgrades(Transform upgradesRoot)
    {
        var abilities = _upgradeOfferGenerator.GenerateOfferAbilities();

        foreach (CharacterAbility ability in abilities)
        {
            UpgradeOfferItemFacade upgradeItemOfferFacade = _upgradeOfferItemFactory.Create(upgradesRoot);
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

    private static string CleanCharacterStatName(string abilityName)
    {
        const string wordToRemove = "Scroll";

        List<string> words = Regex.Split(abilityName, @"(?<!^)(?=[A-Z])")
            .Where(word => !string.IsNullOrEmpty(word))
            .ToList();

        words.Remove(wordToRemove);
        return string.Join(" ", words);
    }
}
