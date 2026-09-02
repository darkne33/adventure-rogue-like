using System;
using System.Collections.Generic;
using Features.Relics.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public sealed class PausePanel : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _leftPanel;
    [SerializeField] private RectTransform _menuContent;
    [SerializeField] private RectTransform _rightPanel;

    [Header("Content")]
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private GameObject _mainButtonsRoot;
    [SerializeField] private GameObject _settingsRoot;
    [FormerlySerializedAs("_inventorySlots")]
    [SerializeField] private CharacterBuildSlotView[] _abilitySlots;
    [SerializeField] private PauseRelicInventoryView _relicInventoryView;
    [SerializeField] private PauseStatRow[] _statRows;

    [Header("Buttons")]
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _exitButton;
    [SerializeField] private Button _settingsBackButton;

    public event Action ResumeRequested;
    public event Action SettingsRequested;
    public event Action RestartRequested;
    public event Action ExitRequested;
    public event Action SettingsBackRequested;

    public CanvasGroup CanvasGroup => _canvasGroup;
    public RectTransform Root => (RectTransform)transform;
    public RectTransform LeftPanel => _leftPanel;
    public RectTransform MenuContent => _menuContent;
    public RectTransform RightPanel => _rightPanel;
    public Button ResumeButton => _resumeButton;
    public Button SettingsBackButton => _settingsBackButton;
    public int AbilitySlotCount => _abilitySlots?.Length ?? 0;

    private void Awake()
    {
        _resumeButton.onClick.AddListener(RequestResume);
        _settingsButton.onClick.AddListener(RequestSettings);
        _restartButton.onClick.AddListener(RequestRestart);
        _exitButton.onClick.AddListener(RequestExit);
        _settingsBackButton.onClick.AddListener(RequestSettingsBack);
    }

    private void OnDestroy()
    {
        _resumeButton.onClick.RemoveListener(RequestResume);
        _settingsButton.onClick.RemoveListener(RequestSettings);
        _restartButton.onClick.RemoveListener(RequestRestart);
        _exitButton.onClick.RemoveListener(RequestExit);
        _settingsBackButton.onClick.RemoveListener(RequestSettingsBack);
    }

    public void SetDescription(string value) =>
        _descriptionText.text = value;

    public void ShowMainButtons()
    {
        _settingsRoot.SetActive(false);
        _mainButtonsRoot.SetActive(true);
    }

    public void ShowSettings()
    {
        _mainButtonsRoot.SetActive(false);
        _settingsRoot.SetActive(true);
    }

    public void SetMainButtonsInteractable(bool interactable)
    {
        _resumeButton.interactable = interactable;
        _settingsButton.interactable = interactable;
        _restartButton.interactable = interactable;
        _exitButton.interactable = interactable;
    }

    public void SetAbilitySlot(int index, Sprite icon, string badge)
    {
        if (index < 0 || index >= AbilitySlotCount)
            return;

        _abilitySlots[index]?.SetContent(icon, badge);
    }

    public void ClearAbilitySlots(int firstIndex = 0)
    {
        for (int index = Mathf.Max(0, firstIndex); index < AbilitySlotCount; index++)
            _abilitySlots[index]?.SetEmpty();
    }

    public void SetRelics(IReadOnlyList<RelicRuntimeState> relics) =>
        _relicInventoryView?.Refresh(relics ?? Array.Empty<RelicRuntimeState>());

    public void HideRelicTooltip() =>
        _relicInventoryView?.HideTooltip();

    // Compatibility wrappers for code that still treats the left-side ability list as inventory.
    public void SetInventorySlot(int index, Sprite icon, string badge) =>
        SetAbilitySlot(index, icon, badge);

    public void ClearInventorySlots(int firstIndex = 0) =>
        ClearAbilitySlots(firstIndex);

    public void SetStats(IReadOnlyList<string> values)
    {
        int count = Mathf.Min(_statRows.Length, values.Count);
        for (int index = 0; index < count; index++)
            _statRows[index].SetValue(values[index]);
    }

    private void RequestResume() => ResumeRequested?.Invoke();
    private void RequestSettings() => SettingsRequested?.Invoke();
    private void RequestRestart() => RestartRequested?.Invoke();
    private void RequestExit() => ExitRequested?.Invoke();
    private void RequestSettingsBack() => SettingsBackRequested?.Invoke();
}
