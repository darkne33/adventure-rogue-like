using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PausePanel : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _leftPanel;
    [SerializeField] private RectTransform _menuContent;

    [Header("Content")]
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private GameObject _mainButtonsRoot;
    [SerializeField] private GameObject _settingsRoot;
    [SerializeField] private CharacterBuildSlotView[] _inventorySlots;
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
    public Button ResumeButton => _resumeButton;
    public Button SettingsBackButton => _settingsBackButton;

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

    public void SetInventorySlot(int index, Sprite icon, string badge)
    {
        if (index < 0 || index >= _inventorySlots.Length)
            return;

        _inventorySlots[index].SetContent(icon, badge);
    }

    public void ClearInventorySlots(int firstIndex = 0)
    {
        for (int index = Mathf.Max(0, firstIndex); index < _inventorySlots.Length; index++)
            _inventorySlots[index].SetEmpty();
    }

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
