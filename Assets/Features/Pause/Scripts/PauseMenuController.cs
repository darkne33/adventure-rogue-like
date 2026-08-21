using System;
using System.Collections.Generic;
using Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Features.Enemies.Scripts.Level.Scripts;
using Features.Relics.Scripts;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zenject;

public sealed class PauseMenuController : ITickable, IDisposable
{
    private const int InventorySlotCount = 10;
    private const float ShowDuration = 0.18f;
    private const float HideDuration = 0.12f;

    private static readonly Color BackdropColor = new(0.005f, 0.075f, 0.095f, 0.9f);
    private static readonly Color PanelColor = new(0.12f, 0.15f, 0.16f, 0.98f);
    private static readonly Color PanelBorderColor = new(0.76f, 0.72f, 0.64f, 1f);
    private static readonly Color ButtonNormalColor = new(0.34f, 0.37f, 0.39f, 1f);
    private static readonly Color ButtonHighlightedColor = new(0.47f, 0.47f, 0.55f, 1f);
    private static readonly Color ButtonPressedColor = new(0.22f, 0.23f, 0.27f, 1f);
    private static readonly Color MutedTextColor = new(0.72f, 0.76f, 0.76f, 0.78f);

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

    private readonly List<TMP_Text> _texts = new();
    private readonly List<Image> _panelImages = new();
    private readonly List<Image> _buttonImages = new();
    private readonly List<Image> _slotFrames = new();
    private readonly List<Button> _mainButtons = new();
    private readonly List<InventorySlot> _inventorySlots = new();
    private readonly List<TMP_Text> _statValues = new();

    private RectTransform _root;
    private RectTransform _leftPanel;
    private RectTransform _menuContent;
    private CanvasGroup _canvasGroup;
    private GameObject _buttonsRoot;
    private GameObject _settingsRoot;
    private TMP_Text _descriptionText;
    private Button _resumeButton;
    private Button _settingsBackButton;
    private TMP_FontAsset _fontAsset;
    private Material _fontMaterial;

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
        RelicManager relicManager)
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

        if (_root != null)
        {
            _root.DOKill();
            _leftPanel?.DOKill();
            _menuContent?.DOKill();
            _canvasGroup?.DOKill();
            UnityEngine.Object.Destroy(_root.gameObject);
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

        if (_root == null)
            BuildView();

        ApplyProjectStyle();
        RefreshInventory();
        RefreshStats();

        _isOpen = true;
        _settingsOpen = false;
        _descriptionText.text = "GAME PAUSED";
        _buttonsRoot.SetActive(true);
        _settingsRoot.SetActive(false);
        SetMainButtonsInteractable(true);

        _pauseService.HandlePause();
        _ownsPause = true;
        _cursorService.ShowUiCursor();

        _root.gameObject.SetActive(true);
        _root.SetAsLastSibling();
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
        _leftPanel.localScale = Vector3.one * 0.97f;
        _menuContent.localScale = Vector3.one * 0.94f;

        _canvasGroup.DOKill();
        _leftPanel.DOKill();
        _menuContent.DOKill();
        _canvasGroup.DOFade(1f, ShowDuration).SetUpdate(true);
        _leftPanel.DOScale(1f, ShowDuration).SetEase(Ease.OutBack).SetUpdate(true);
        _menuContent.DOScale(1f, ShowDuration).SetEase(Ease.OutBack).SetUpdate(true);

        Select(_resumeButton);
    }

    private void Resume()
    {
        if (!_isOpen || _isRestarting || _runRestartService.IsRestarting)
            return;

        _isOpen = false;
        _settingsOpen = false;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        ReleaseOwnedPause();
        _cursorService.ShowGameplayCursor();

        _canvasGroup.DOKill();
        _canvasGroup
            .DOFade(0f, HideDuration)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (_root != null && !_isOpen)
                    _root.gameObject.SetActive(false);
            });
    }

    private void ShowSettings()
    {
        if (!_isOpen || _isRestarting || _runRestartService.IsRestarting)
            return;

        _settingsOpen = true;
        _buttonsRoot.SetActive(false);
        _settingsRoot.SetActive(true);
        Select(_settingsBackButton);
    }

    private void ShowMainButtons()
    {
        if (!_isOpen)
            return;

        _settingsOpen = false;
        _settingsRoot.SetActive(false);
        _buttonsRoot.SetActive(true);
        Select(_resumeButton);
    }

    private void RequestRestart()
    {
        if (_isRestarting || _runRestartService.IsRestarting)
            return;

        if (_roomTransitionService.IsPlaying)
        {
            _descriptionText.text = "WAIT FOR TRANSITION";
            return;
        }

        RestartRun().Forget();
    }

    private async UniTaskVoid RestartRun()
    {
        _isRestarting = true;
        _descriptionText.text = "RESTARTING RUN...";
        SetMainButtonsInteractable(false);

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

            if (!_disposed && _root != null)
            {
                if (!_timeScaleService.IsPaused)
                {
                    _pauseService.HandlePause();
                    _ownsPause = true;
                }

                _isOpen = true;
                _descriptionText.text = "RESTART FAILED";
                SetMainButtonsInteractable(true);
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

    private void SetMainButtonsInteractable(bool interactable)
    {
        foreach (Button button in _mainButtons)
            button.interactable = interactable;
    }

    private static void Select(Button button)
    {
        if (button != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    private void RefreshInventory()
    {
        int slotIndex = 0;

        foreach (UpgradeBuildEntry entry in _upgradeBuildService.SelectedUpgrades)
        {
            if (slotIndex >= _inventorySlots.Count)
                break;

            _inventorySlots[slotIndex++].Set(entry.Ability?.Icon,
                entry.Ability == null ? string.Empty : $"LVL {entry.Level}");
        }

        foreach (RelicRuntimeState relic in _relicManager.ActiveRelics)
        {
            if (slotIndex >= _inventorySlots.Count)
                break;

            _inventorySlots[slotIndex++].Set(relic.Definition?.Icon,
                relic.IsBroken ? "BROKEN" : $"x{relic.StackCount}");
        }

        while (slotIndex < _inventorySlots.Count)
            _inventorySlots[slotIndex++].Clear();
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

        for (int index = 0; index < _statValues.Count && index < values.Length; index++)
            _statValues[index].text = values[index];
    }

    private void BuildView()
    {
        Transform popupRoot = _panelsProvider.GetRootFor(PanelLocation.PopUp);
        _fontAsset = TMP_Settings.defaultFontAsset;

        _root = CreateRect("PauseMenu", popupRoot);
        Stretch(_root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        _canvasGroup = _root.gameObject.AddComponent<CanvasGroup>();

        RectTransform backdrop = CreateRect("Backdrop", _root);
        Stretch(backdrop, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image backdropImage = AddImage(backdrop, BackdropColor, true);
        backdropImage.raycastTarget = true;

        BuildLeftPanel();
        BuildMenuContent();
        BuildShrineLogs();

        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        _root.gameObject.SetActive(false);
    }

    private void BuildLeftPanel()
    {
        _leftPanel = CreateRect("InventoryAndStats", _root);
        Stretch(_leftPanel, new Vector2(0.025f, 0.06f), new Vector2(0.30f, 0.91f),
            Vector2.zero, Vector2.zero);
        AddPanelImage(_leftPanel);

        RectTransform header = CreateRect("InventoryHeader", _leftPanel);
        Stretch(header, new Vector2(0.025f, 0.90f), new Vector2(0.975f, 0.985f),
            Vector2.zero, Vector2.zero);
        AddImage(header, new Color(0.13f, 0.27f, 0.23f, 0.98f), false);
        AddOutline(header.gameObject, PanelBorderColor, new Vector2(4f, -4f));
        CreateText("INVENTORY", header, 66f, TextAlignmentOptions.MidlineLeft,
            Color.white, new Vector2(0.04f, 0f), new Vector2(0.96f, 1f));

        CreateText("BUILD & RELICS", _leftPanel, 38f, TextAlignmentOptions.Center,
            MutedTextColor, new Vector2(0.08f, 0.845f), new Vector2(0.92f, 0.895f));

        RectTransform inventoryGrid = CreateRect("InventoryGrid", _leftPanel);
        Stretch(inventoryGrid, new Vector2(0.075f, 0.60f), new Vector2(0.925f, 0.845f),
            Vector2.zero, Vector2.zero);
        GridLayoutGroup grid = inventoryGrid.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(160f, 160f);
        grid.spacing = new Vector2(24f, 24f);
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;

        for (int index = 0; index < InventorySlotCount; index++)
            _inventorySlots.Add(CreateInventorySlot(inventoryGrid, index));

        RectTransform statsHeader = CreateRect("StatsHeader", _leftPanel);
        Stretch(statsHeader, new Vector2(0.04f, 0.535f), new Vector2(0.96f, 0.59f),
            Vector2.zero, Vector2.zero);
        AddImage(statsHeader, new Color(0.08f, 0.11f, 0.11f, 0.9f), false);
        CreateText("CHARACTER STATS", statsHeader, 44f, TextAlignmentOptions.Center,
            Color.white, Vector2.zero, Vector2.one);

        string[] labels =
        {
            "HEALTH", "MAX HEALTH", "HEALTH REGEN", "SHIELD", "EVASION",
            "ARMOR", "LIFESTEAL", "HEAL ON KILL", "THORNS", "DAMAGE",
            "ATTACK SPEED", "PROJECTILES", "COOLDOWN", "CRIT CHANCE",
            "CRIT DAMAGE", "ABILITY DURATION", "PICKUP RANGE", "MOVE SPEED",
            "JUMP FORCE", "LUCK", "GOLD GAIN", "XP GAIN"
        };

        RectTransform statsRoot = CreateRect("Stats", _leftPanel);
        Stretch(statsRoot, new Vector2(0.075f, 0.065f), new Vector2(0.925f, 0.52f),
            Vector2.zero, Vector2.zero);

        for (int index = 0; index < labels.Length; index++)
            CreateStatRow(statsRoot, labels[index], index);
    }

    private InventorySlot CreateInventorySlot(Transform parent, int index)
    {
        RectTransform slot = CreateRect($"InventorySlot_{index + 1}", parent);
        Image frame = AddImage(slot, new Color(0.18f, 0.20f, 0.20f, 0.96f), false);
        _slotFrames.Add(frame);
        AddOutline(slot.gameObject, new Color(0.58f, 0.55f, 0.49f, 0.9f),
            new Vector2(3f, -3f));

        RectTransform iconRect = CreateRect("Icon", slot);
        Stretch(iconRect, new Vector2(0.13f, 0.19f), new Vector2(0.87f, 0.88f),
            Vector2.zero, Vector2.zero);
        Image icon = AddImage(iconRect, Color.white, false);
        icon.preserveAspect = true;
        icon.enabled = false;

        TMP_Text emptyText = CreateText("+", slot, 58f, TextAlignmentOptions.Center,
            new Color(0.55f, 0.56f, 0.55f, 0.42f), new Vector2(0.1f, 0.14f),
            new Vector2(0.9f, 0.9f));

        TMP_Text badge = CreateText(string.Empty, slot, 27f, TextAlignmentOptions.Center,
            Color.white, new Vector2(0.04f, 0.01f), new Vector2(0.96f, 0.22f));

        return new InventorySlot(icon, badge, emptyText);
    }

    private void BuildShrineLogs()
    {
        RectTransform shrineLogs = CreateRect("ShrineLogs", _root);
        Stretch(shrineLogs, new Vector2(0.73f, 0.58f), new Vector2(0.975f, 0.88f),
            Vector2.zero, Vector2.zero);

        CreateText("SHRINE LOGS", shrineLogs, 56f, TextAlignmentOptions.Center,
            Color.white, new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.98f));

        RectTransform logBody = CreateRect("LogBody", shrineLogs);
        Stretch(logBody, new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.73f),
            Vector2.zero, Vector2.zero);
        AddImage(logBody, new Color(0.02f, 0.08f, 0.09f, 0.52f), false);
        AddOutline(logBody.gameObject, new Color(0.42f, 0.48f, 0.47f, 0.42f),
            new Vector2(2f, -2f));

        CreateText("NO SHRINES DISCOVERED", logBody, 32f, TextAlignmentOptions.Center,
            MutedTextColor, new Vector2(0.08f, 0.10f), new Vector2(0.92f, 0.90f));
    }

    private void CreateStatRow(RectTransform parent, string label, int index)
    {
        const float rowHeight = 36f;
        RectTransform row = CreateRect($"Stat_{label.Replace(" ", string.Empty)}", parent);
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = new Vector2(1f, 1f);
        row.pivot = new Vector2(0.5f, 1f);
        row.anchoredPosition = new Vector2(0f, -index * rowHeight);
        row.sizeDelta = new Vector2(0f, rowHeight);

        if (index % 2 == 1)
            AddImage(row, new Color(1f, 1f, 1f, 0.025f), false);

        CreateText(label, row, 25f, TextAlignmentOptions.MidlineLeft, Color.white,
            new Vector2(0.02f, 0f), new Vector2(0.70f, 1f));
        TMP_Text value = CreateText("--", row, 25f, TextAlignmentOptions.MidlineRight,
            Color.white, new Vector2(0.68f, 0f), new Vector2(0.98f, 1f));
        _statValues.Add(value);
    }

    private void BuildMenuContent()
    {
        _menuContent = CreateRect("MenuContent", _root);
        Stretch(_menuContent, new Vector2(0.34f, 0.10f), new Vector2(0.69f, 0.92f),
            Vector2.zero, Vector2.zero);

        TMP_Text title = CreateText("PAUSED", _menuContent, 190f,
            TextAlignmentOptions.Center, Color.white, new Vector2(0f, 0.76f),
            new Vector2(1f, 1f));
        title.enableAutoSizing = true;
        title.fontSizeMin = 90f;
        AddShadow(title.gameObject, Color.black, new Vector2(12f, -12f));

        _descriptionText = CreateText("GAME PAUSED", _menuContent, 42f,
            TextAlignmentOptions.Center, MutedTextColor, new Vector2(0.1f, 0.70f),
            new Vector2(0.9f, 0.79f));

        _buttonsRoot = CreateRect("Buttons", _menuContent).gameObject;
        RectTransform buttonsRect = (RectTransform)_buttonsRoot.transform;
        Stretch(buttonsRect, new Vector2(0.20f, 0.08f), new Vector2(0.80f, 0.68f),
            Vector2.zero, Vector2.zero);

        _resumeButton = CreateMenuButton("RESUME", buttonsRect, new Vector2(0f, 285f), Resume);
        CreateMenuButton("SETTINGS", buttonsRect, new Vector2(0f, 95f), ShowSettings);
        CreateMenuButton("RESTART", buttonsRect, new Vector2(0f, -95f), RequestRestart);
        CreateMenuButton("EXIT", buttonsRect, new Vector2(0f, -285f), ExitGame);

        BuildSettingsPanel();
    }

    private void BuildSettingsPanel()
    {
        RectTransform settings = CreateRect("Settings", _menuContent);
        Stretch(settings, new Vector2(0.17f, 0.14f), new Vector2(0.83f, 0.67f),
            Vector2.zero, Vector2.zero);
        AddPanelImage(settings);
        _settingsRoot = settings.gameObject;

        CreateText("SETTINGS", settings, 68f, TextAlignmentOptions.Center,
            Color.white, new Vector2(0.08f, 0.77f), new Vector2(0.92f, 0.96f));

        CreateText("SETTINGS ARE COMING SOON", settings, 36f, TextAlignmentOptions.Center,
            MutedTextColor, new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.68f));

        _settingsBackButton = CreateMenuButton("BACK", settings, new Vector2(0f, -125f), ShowMainButtons,
            new Vector2(400f, 104f));
        _settingsRoot.SetActive(false);
    }

    private Button CreateMenuButton(string label, Transform parent, Vector2 position,
        UnityEngine.Events.UnityAction action, Vector2? size = null)
    {
        RectTransform rect = CreateRect(label.Replace(" ", string.Empty) + "Button", parent);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size ?? new Vector2(520f, 138f);

        Image image = AddImage(rect, ButtonNormalColor, true);
        _buttonImages.Add(image);
        AddOutline(rect.gameObject, PanelBorderColor, new Vector2(4f, -4f));
        AddShadow(rect.gameObject, new Color(0f, 0f, 0f, 0.9f), new Vector2(9f, -11f));

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = new ColorBlock
        {
            normalColor = ButtonNormalColor,
            highlightedColor = ButtonHighlightedColor,
            pressedColor = ButtonPressedColor,
            selectedColor = ButtonHighlightedColor,
            disabledColor = new Color(0.20f, 0.21f, 0.22f, 0.65f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f,
        };
        button.onClick.AddListener(action);
        rect.gameObject.AddComponent<UIButtonJuice>();

        TMP_Text text = CreateText(label, rect, 57f, TextAlignmentOptions.Center,
            Color.white, new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.95f));
        AddShadow(text.gameObject, Color.black, new Vector2(5f, -5f));

        AddButtonCorners(rect);
        _mainButtons.Add(button);
        return button;
    }

    private void AddButtonCorners(RectTransform button)
    {
        Vector2[] anchors =
        {
            new(0f, 0f), new(0f, 1f), new(1f, 0f), new(1f, 1f)
        };

        foreach (Vector2 anchor in anchors)
        {
            RectTransform corner = CreateRect("Corner", button);
            corner.anchorMin = corner.anchorMax = anchor;
            corner.pivot = anchor;
            corner.anchoredPosition = Vector2.zero;
            corner.sizeDelta = new Vector2(22f, 22f);
            AddImage(corner, PanelBorderColor, false);
        }
    }

    private void ApplyProjectStyle()
    {
        CharacterPanel characterPanel = _panelsProvider.GetOpenedPanel<CharacterPanel>();
        if (characterPanel == null)
            return;

        TMP_Text templateText = characterPanel.GetComponentInChildren<TMP_Text>(true);
        if (templateText != null)
        {
            _fontAsset = templateText.font;
            _fontMaterial = templateText.fontSharedMaterial;

            foreach (TMP_Text text in _texts)
            {
                text.font = _fontAsset;
                if (_fontMaterial != null)
                    text.fontSharedMaterial = _fontMaterial;
            }
        }

        Image[] projectImages = characterPanel.GetComponentsInChildren<Image>(true);
        Sprite buttonSprite = FindSprite(projectImages, "UpgradeButtonGreen");
        Sprite panelSprite = FindSprite(projectImages, "Border All 6 Cell 65");
        Sprite slotSprite = FindSprite(projectImages, "Cell 01", "New");

        if (buttonSprite != null)
        {
            foreach (Image image in _buttonImages)
            {
                image.sprite = buttonSprite;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1f;
            }
        }

        if (panelSprite != null)
        {
            foreach (Image image in _panelImages)
            {
                image.sprite = panelSprite;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 13f;
            }
        }

        Sprite resolvedSlotSprite = slotSprite != null ? slotSprite : panelSprite;
        if (resolvedSlotSprite == null)
            return;

        foreach (Image image in _slotFrames)
        {
            image.sprite = resolvedSlotSprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = slotSprite != null ? 7f : 13f;
        }
    }

    private static Sprite FindSprite(IEnumerable<Image> images, params string[] nameParts)
    {
        foreach (Image image in images)
        {
            Sprite sprite = image.sprite;
            if (sprite == null)
                continue;

            bool matches = true;
            foreach (string part in nameParts)
            {
                if (sprite.name.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                matches = false;
                break;
            }

            if (matches)
                return sprite;
        }

        return null;
    }

    private Image AddPanelImage(RectTransform rect)
    {
        Image image = AddImage(rect, PanelColor, false);
        _panelImages.Add(image);
        AddOutline(rect.gameObject, PanelBorderColor, new Vector2(5f, -5f));
        AddShadow(rect.gameObject, new Color(0f, 0f, 0f, 0.85f), new Vector2(10f, -12f));
        return image;
    }

    private TMP_Text CreateText(string value, Transform parent, float fontSize,
        TextAlignmentOptions alignment, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        RectTransform rect = CreateRect("Label", parent);
        Stretch(rect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);

        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontSizeMax = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(18f, fontSize * 0.55f);
        text.fontStyle = FontStyles.UpperCase;

        if (_fontAsset != null)
            text.font = _fontAsset;
        if (_fontMaterial != null)
            text.fontSharedMaterial = _fontMaterial;

        _texts.Add(text);
        return text;
    }

    private RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject uiObject = new(objectName, typeof(RectTransform));
        uiObject.layer = parent.gameObject.layer;
        RectTransform rect = (RectTransform)uiObject.transform;
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        return rect;
    }

    private static Image AddImage(RectTransform rect, Color color, bool raycastTarget)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    private static void AddOutline(GameObject target, Color color, Vector2 distance)
    {
        UnityEngine.UI.Outline outline =
            target.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
    }

    private static void AddShadow(GameObject target, Color color, Vector2 distance)
    {
        Shadow shadow = target.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private sealed class InventorySlot
    {
        private readonly Image _icon;
        private readonly TMP_Text _badge;
        private readonly TMP_Text _emptyText;

        public InventorySlot(Image icon, TMP_Text badge, TMP_Text emptyText)
        {
            _icon = icon;
            _badge = badge;
            _emptyText = emptyText;
        }

        public void Set(Sprite sprite, string badge)
        {
            _icon.sprite = sprite;
            _icon.enabled = sprite != null;
            _badge.text = badge;
            _emptyText.gameObject.SetActive(sprite == null);
        }

        public void Clear()
        {
            _icon.sprite = null;
            _icon.enabled = false;
            _badge.text = string.Empty;
            _emptyText.gameObject.SetActive(true);
        }
    }
}
