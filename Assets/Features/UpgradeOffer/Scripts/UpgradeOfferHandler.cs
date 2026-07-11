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
    private readonly UpgradeOfferConfiguration _upgradeOfferConfiguration;
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
        IUpgradeOfferItemFactory upgradeOfferItemFactory, UpgradeOfferConfiguration upgradeOfferConfiguration,
        IPanelService panelService, CharacterStats characterStats, ICharacterProvider characterProvider,
        IPauseService pauseService, ICharacterLevelService characterLevelService)
    {
        _upgradeOfferGenerator = upgradeOfferGenerator;
        _upgradeOfferItemFactory = upgradeOfferItemFactory;
        _upgradeOfferConfiguration = upgradeOfferConfiguration;
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
        PlayItemsAppearance();
    }

    public void SkipUpgrades()
    {
        if (_isOpen == false || _isClosing)
            return;

        _isClosing = true;
        CloseCurrentOffer().Forget();
    }

    public void ApplyUpgradeOffer(UpgradeOffer upgradeOffer)
    {
        _characterProvider.CharacterFacade.CharacterAbilitySystem.AddAbility(upgradeOffer.Ability, _characterStats,
            upgradeOffer.UpgradeMultiplier, upgradeOffer.UpgradeType);
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
        PlayItemsAppearance();
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
        {
            upgradeItem.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(upgradeItem.gameObject);
        }

        _upgradeItems.Clear();
    }

    private void GenerateUpgrades(Transform upgradesRoot)
    {
        var upgradeOffers = _upgradeOfferGenerator.GenerateOffers();

        foreach (UpgradeOffer upgradeOffer in upgradeOffers)
        {
            CharacterAbility ability = upgradeOffer.Ability;
            UpgradeOfferItemFacade upgradeItemOfferFacade = _upgradeOfferItemFactory.Create(upgradesRoot);
            UpgradeOfferItemView offerItemView = upgradeItemOfferFacade.UpgradeOfferItemView;

            offerItemView.DeactivateSkillsDescriptions();
            offerItemView.SetupName(ability.DisplayName);
            if (upgradeOffer.HasRarity)
                offerItemView.SetupRarity(upgradeOffer.Rarity);
            else
                offerItemView.HideRarity();

            offerItemView.SetupSprites(_upgradeOfferConfiguration.GetItemSpriteSet(upgradeOffer.ItemType));
            offerItemView.SetupIcon(ability.Icon);
            offerItemView.SetupLevel(ability.Level, ability.IsAcquired);

            switch (ability)
            {
                case CharacterPassiveAbility passiveAbility:
                    string statName = CleanCharacterStatName(passiveAbility.Id.ToString());
                    float statFrom = passiveAbility.GetStatFromIncrease(_characterStats);
                    float statTo = passiveAbility.GetStatToIncrease(_characterStats,
                        upgradeOffer.UpgradeMultiplier);
                    offerItemView.SetupSkillDescription_1(
                        new AbilityUpgradePreview(statName, statFrom, statTo, passiveAbility.StatSuffix));
                    break;
                case CharacterActiveAbility activeAbility:
                    AbilityUpgradePreview[] previews = upgradeOffer.HasRarity
                        ? activeAbility.GetUpgradePreviews(upgradeOffer.UpgradeType, upgradeOffer.UpgradeMultiplier)
                        : activeAbility.GetAcquirePreviews();

                    if (previews.Length > 0)
                        offerItemView.SetupSkillDescription_1(previews[0]);
                    if (previews.Length > 1)
                        offerItemView.SetupSkillDescription_2(previews[1]);
                    break;
            }

            _upgradeItems.Add(offerItemView);
            upgradeItemOfferFacade.UpgradeOfferItemApplyHandler.Initialize(upgradeOffer);
        }
    }

    private void PlayItemsAppearance()
    {
        for (int i = 0; i < _upgradeItems.Count; i++)
            _upgradeItems[i].PlayAppearance();
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
