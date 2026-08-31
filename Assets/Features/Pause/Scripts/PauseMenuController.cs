using System;
using Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Features.Enemies.Scripts.Level.Scripts;
using Features.Relics.Scripts;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Zenject;

public sealed class PauseMenuController : ITickable, IDisposable
{
    private const int InventorySlotCount = 10;
    private const float ShowDuration = 0.18f;
    private const float HideDuration = 0.12f;

    private readonly IPanelsProvider _panelsProvider;
    private readonly IPauseService _pauseService;
    private readonly ITimeScaleService _timeScaleService;
    private readonly ICursorService _cursorService;
    private readonly RunRestartService _runRestartService;
    private readonly ISceneService<RogueLikeSceneProvider> _sceneService;
    private readonly IRoomTransitionService _roomTransitionService;
    private readonly ICharacterProvider _characterProvider;
    private readonly CharacterStats _characterStats;
    private readonly UpgradeBuildService _upgradeBuildService;
    private readonly RelicManager _relicManager;
    private readonly PausePanel _pausePanelPrefab;
    private readonly DiContainer _container;

    private PausePanel _panel;

    private bool _isOpen;
    private bool _ownsPause;
    private bool _settingsOpen;
    private bool _isRestarting;
    private bool _disposed;

    public PauseMenuController(
        IPanelsProvider panelsProvider,
        IPauseService pauseService,
        ITimeScaleService timeScaleService,
        ICursorService cursorService,
        RunRestartService runRestartService,
        ISceneService<RogueLikeSceneProvider> sceneService,
        IRoomTransitionService roomTransitionService,
        ICharacterProvider characterProvider,
        CharacterStats characterStats,
        UpgradeBuildService upgradeBuildService,
        RelicManager relicManager,
        PausePanel pausePanelPrefab,
        DiContainer container)
    {
        _panelsProvider = panelsProvider;
        _pauseService = pauseService;
        _timeScaleService = timeScaleService;
        _cursorService = cursorService;
        _runRestartService = runRestartService;
        _sceneService = sceneService;
        _roomTransitionService = roomTransitionService;
        _characterProvider = characterProvider;
        _characterStats = characterStats;
        _upgradeBuildService = upgradeBuildService;
        _relicManager = relicManager;
        _pausePanelPrefab = pausePanelPrefab != null
            ? pausePanelPrefab
            : throw new ArgumentNullException(nameof(pausePanelPrefab));
        _container = container;
    }

    public void Tick()
    {
        bool escapePressed = Keyboard.current != null &&
                             Keyboard.current.escapeKey.wasPressedThisFrame;
        bool startPressed = Gamepad.current != null &&
                            Gamepad.current.startButton.wasPressedThisFrame;
        bool gamepadCancelPressed = _isOpen && Gamepad.current != null &&
                                    Gamepad.current.buttonEast.wasPressedThisFrame;
        if (!escapePressed && !startPressed && !gamepadCancelPressed)
            return;

        if (_isRestarting || _runRestartService.IsRestarting)
            return;

        if (_settingsOpen)
        {
            ShowMainButtons();
            return;
        }

        if (_isOpen)
        {
            Resume();
            return;
        }

        TryOpen();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        bool wasOpen = _isOpen;
        ReleaseOwnedPause();
        if (wasOpen)
            _cursorService.ShowGameplayCursor();

        if (_panel != null)
        {
            UnsubscribeFromView();
            _panel.Root.DOKill();
            _panel.LeftPanel.DOKill();
            _panel.MenuContent.DOKill();
            _panel.CanvasGroup.DOKill();
            UnityEngine.Object.Destroy(_panel.gameObject);
            _panel = null;
        }
    }

    private void TryOpen()
    {
        if (_disposed || _runRestartService.IsRestarting ||
            _roomTransitionService.IsPlaying || _timeScaleService.IsPaused ||
            _characterProvider.CharacterFacade == null)
            return;

        CharacterPanel characterPanel = _panelsProvider.GetOpenedPanel<CharacterPanel>();
        if (characterPanel == null)
            return;

        if (_panel == null)
            CreateView();

        RefreshInventory();
        RefreshStats();

        _isOpen = true;
        _settingsOpen = false;
        _panel.SetDescription("GAME PAUSED");
        _panel.ShowMainButtons();
        _panel.SetMainButtonsInteractable(true);

        _pauseService.HandlePause();
        _ownsPause = true;
        _cursorService.ShowUiCursor();

        _panel.gameObject.SetActive(true);
        _panel.Root.SetAsLastSibling();
        _panel.CanvasGroup.alpha = 0f;
        _panel.CanvasGroup.interactable = true;
        _panel.CanvasGroup.blocksRaycasts = true;
        _panel.LeftPanel.localScale = Vector3.one * 0.97f;
        _panel.MenuContent.localScale = Vector3.one * 0.94f;

        _panel.CanvasGroup.DOKill();
        _panel.LeftPanel.DOKill();
        _panel.MenuContent.DOKill();
        _panel.CanvasGroup.DOFade(1f, ShowDuration).SetUpdate(true);
        _panel.LeftPanel.DOScale(1f, ShowDuration).SetEase(Ease.OutBack).SetUpdate(true);
        _panel.MenuContent.DOScale(1f, ShowDuration).SetEase(Ease.OutBack).SetUpdate(true);

        Select(_panel.ResumeButton);
    }

    private void Resume()
    {
        if (!_isOpen || _isRestarting || _runRestartService.IsRestarting)
            return;

        _isOpen = false;
        _settingsOpen = false;
        _panel.CanvasGroup.interactable = false;
        _panel.CanvasGroup.blocksRaycasts = false;

        ReleaseOwnedPause();
        _cursorService.ShowGameplayCursor();

        _panel.CanvasGroup.DOKill();
        _panel.CanvasGroup
            .DOFade(0f, HideDuration)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (_panel != null && !_isOpen)
                    _panel.gameObject.SetActive(false);
            });
    }

    private void ShowSettings()
    {
        if (!_isOpen || _isRestarting || _runRestartService.IsRestarting)
            return;

        _settingsOpen = true;
        _panel.ShowSettings();
        Select(_panel.SettingsBackButton);
    }

    private void ShowMainButtons()
    {
        if (!_isOpen)
            return;

        _settingsOpen = false;
        _panel.ShowMainButtons();
        Select(_panel.ResumeButton);
    }

    private void RequestRestart()
    {
        if (_isRestarting || _runRestartService.IsRestarting)
            return;

        if (_roomTransitionService.IsPlaying)
        {
            _panel.SetDescription("WAIT FOR TRANSITION");
            return;
        }

        RestartRun().Forget();
    }

    private async UniTaskVoid RestartRun()
    {
        _isRestarting = true;
        _panel.SetDescription("RESTARTING RUN...");
        _panel.SetMainButtonsInteractable(false);

        try
        {
            string sceneName = _sceneService.GameSceneComponentsService.gameObject.scene.name;

            bool restarted = await _runRestartService.Restart(sceneName);
            if (!restarted)
                throw _runRestartService.LastError ??
                      new InvalidOperationException("A run restart is already in progress.");
        }
        catch (Exception exception)
        {
            if (exception != _runRestartService.LastError)
                Debug.LogException(exception);

            if (!_disposed && _panel != null)
            {
                if (!_timeScaleService.IsPaused)
                {
                    _pauseService.HandlePause();
                    _ownsPause = true;
                }

                _isOpen = true;
                _panel.SetDescription("RESTART FAILED");
                _panel.SetMainButtonsInteractable(true);
                _cursorService.ShowUiCursor();
            }
        }
        finally
        {
            _isRestarting = false;
        }
    }

    private void ExitGame()
    {
        if (_isRestarting || _runRestartService.IsRestarting)
            return;

        Application.Quit();
    }

    private void ReleaseOwnedPause()
    {
        if (!_ownsPause)
            return;

        _pauseService.CancelPause();
        _ownsPause = false;
    }

    private static void Select(UnityEngine.UI.Button button)
    {
        if (button != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    private void RefreshInventory()
    {
        int slotIndex = 0;

        foreach (UpgradeBuildEntry entry in _upgradeBuildService.SelectedUpgrades)
        {
            if (slotIndex >= InventorySlotCount)
                break;

            _panel.SetInventorySlot(slotIndex++, entry.Ability?.Icon,
                entry.Ability == null ? string.Empty : $"LVL {entry.Level}");
        }

        foreach (RelicRuntimeState relic in _relicManager.ActiveRelics)
        {
            if (slotIndex >= InventorySlotCount)
                break;

            _panel.SetInventorySlot(slotIndex++, relic.Definition?.Icon,
                relic.IsBroken ? "BROKEN" : $"x{relic.StackCount}");
        }

        _panel.ClearInventorySlots(slotIndex);
    }

    private void RefreshStats()
    {
        HealthSystem health = _characterProvider.CharacterFacade?.HealthSystem;
        float armorRating = Mathf.Max(0f, _characterStats.Armor);
        float armorReduction = (1f - 100f / (100f + armorRating)) * 100f;
        string[] values =
        {
            health == null ? "--" : $"{health.CurrentHealth:0}/{health.MaxHealth:0}",
            $"{_characterStats.MaxHp:0}",
            $"{_characterStats.RegenHp:0.0}/s",
            $"{_characterStats.Shield:0}",
            $"{_characterStats.Evasion:0.#}%",
            $"{armorReduction:0.#}%",
            $"{_characterStats.LifeSteal:0.#}%",
            $"{_characterStats.GainHp:0.#}",
            $"{_characterStats.ThornsDamage:0.#}",
            $"{1f + _characterStats.DamageInPercent * 0.01f:0.00}x",
            $"{100f + _characterStats.AttackSpeed:0.#}%",
            $"{Mathf.RoundToInt(_characterStats.ProjectileCount)}",
            $"{_characterStats.CooldownReduction:0.#}%",
            $"{_characterStats.CritChance:0.#}%",
            $"{1f + _characterStats.CritDamage * 0.01f:0.00}x",
            $"{1f + _characterStats.AbilityDuration * 0.01f:0.00}x",
            $"{1f + _characterStats.PickupRange * 0.01f:0.00}x",
            $"{_characterStats.MovementSpeed:0.00}",
            $"{_characterStats.JumpForce:0.00}",
            $"{_characterStats.Luck:0.#}%",
            $"{1f + _characterStats.GainGold * 0.01f:0.00}x",
            $"{1f + _characterStats.XPBonus * 0.01f:0.00}x",
        };

        _panel.SetStats(values);
    }

    private void CreateView()
    {
        Transform popupRoot = _panelsProvider.GetRootFor(PanelLocation.PopUp);
        _panel = _container.InstantiatePrefabForComponent<PausePanel>(_pausePanelPrefab, popupRoot);
        _panel.ResumeRequested += Resume;
        _panel.SettingsRequested += ShowSettings;
        _panel.RestartRequested += RequestRestart;
        _panel.ExitRequested += ExitGame;
        _panel.SettingsBackRequested += ShowMainButtons;
        _panel.CanvasGroup.alpha = 0f;
        _panel.CanvasGroup.interactable = false;
        _panel.CanvasGroup.blocksRaycasts = false;
        _panel.gameObject.SetActive(false);
    }

    private void UnsubscribeFromView()
    {
        _panel.ResumeRequested -= Resume;
        _panel.SettingsRequested -= ShowSettings;
        _panel.RestartRequested -= RequestRestart;
        _panel.ExitRequested -= ExitGame;
        _panel.SettingsBackRequested -= ShowMainButtons;
    }
}
