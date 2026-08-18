using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class CharacterSelectionView : MonoBehaviour
{
    [Header("Roster")]
    [SerializeField] private Sprite _portraitPlaceholder;
    [SerializeField] private HorizontalLayoutGroup _portraitLayout;
    [SerializeField] private CharacterPortraitSlotView[] _portraitSlots;

    [Header("Character")]
    [SerializeField] private Image _selectedPortrait;
    [SerializeField] private TMP_Text _characterName;
    [SerializeField] private TMP_Text _characterCounter;
    [SerializeField] private TMP_Text _description;

    [Header("Stats")]
    [SerializeField] private TMP_Text _healthValue;
    [SerializeField] private TMP_Text _damageValue;
    [SerializeField] private TMP_Text _attackSpeedValue;
    [SerializeField] private TMP_Text _movementSpeedValue;

    [Header("Navigation")]
    [SerializeField] private Button _previousButton;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _backButton;

    private IReadOnlyList<CharacterDefinition> _characters;
    private int _selectedIndex;
    private bool _isInteractable;

    public event Action<int> SelectionRequested;
    public event Action StartRequested;
    public event Action BackRequested;

    public void Show(IReadOnlyList<CharacterDefinition> characters, int selectedIndex)
    {
        if (characters == null || characters.Count == 0)
            throw new InvalidOperationException("Character selection requires at least one configured character.");

        if (_portraitSlots == null || _portraitSlots.Length == 0)
            throw new InvalidOperationException("Character selection prefab does not contain portrait slots.");

        _characters = characters;
        gameObject.SetActive(true);
        SetSelectedIndex(selectedIndex);
        SetInteractable(true);
        FocusStart();
    }

    public void Hide() =>
        gameObject.SetActive(false);

    public void SetSelectedIndex(int index)
    {
        if (_characters == null || _characters.Count == 0)
            return;

        _selectedIndex = WrapIndex(index);
        RefreshPortraitSlots();
        RefreshCharacterDetails();
    }

    public void SetInteractable(bool interactable)
    {
        _isInteractable = interactable;
        _previousButton.interactable = interactable;
        _nextButton.interactable = interactable;
        _startButton.interactable = interactable &&
            _characters is { Count: > 0 } && _characters[_selectedIndex].IsConfigured;
        _backButton.interactable = interactable;

        foreach (CharacterPortraitSlotView slot in _portraitSlots)
            slot.SetInteractable(interactable);
    }

    public void HandleMove(AxisEventData eventData)
    {
        switch (eventData.moveDir)
        {
            case MoveDirection.Left:
                RequestPreviousCharacter();
                break;
            case MoveDirection.Right:
                RequestNextCharacter();
                break;
            case MoveDirection.Up:
                FocusStart();
                break;
            case MoveDirection.Down:
                FocusBack();
                break;
            default:
                return;
        }

        eventData.Use();
    }

    public void RequestBackFromInput(BaseEventData eventData)
    {
        eventData.Use();
        RequestBack();
    }

    public void RequestPreviousCharacter() =>
        RequestRelativeSelection(-1);

    public void RequestNextCharacter() =>
        RequestRelativeSelection(1);

    public void RequestStart() =>
        StartRequested?.Invoke();

    public void RequestBack() =>
        BackRequested?.Invoke();

    internal void RequestSelectionFromSlot(int index) =>
        RequestSelection(index);

    private void RefreshPortraitSlots()
    {
        int visibleCount = Mathf.Min(_portraitSlots.Length, _characters.Count);
        int firstCharacterIndex = _characters.Count <= _portraitSlots.Length
            ? 0
            : Mathf.Clamp(_selectedIndex - _portraitSlots.Length / 2,
                0, _characters.Count - _portraitSlots.Length);
        float availableWidth = 1100f - _portraitLayout.spacing * Mathf.Max(0, visibleCount - 1);
        float slotWidth = Mathf.Clamp(availableWidth / visibleCount, 84f, 190f);

        for (int slotIndex = 0; slotIndex < _portraitSlots.Length; slotIndex++)
        {
            CharacterPortraitSlotView slot = _portraitSlots[slotIndex];
            if (slotIndex >= visibleCount)
            {
                slot.Clear();
                continue;
            }

            int characterIndex = firstCharacterIndex + slotIndex;
            slot.Bind(characterIndex, _characters[characterIndex], _portraitPlaceholder, slotWidth);
            slot.SetSelected(characterIndex == _selectedIndex);
            slot.SetInteractable(_isInteractable);
        }
    }

    private void RefreshCharacterDetails()
    {
        CharacterDefinition character = _characters[_selectedIndex];
        Sprite portrait = character.Portrait != null ? character.Portrait : _portraitPlaceholder;

        _selectedPortrait.sprite = portrait;
        _selectedPortrait.enabled = portrait != null;
        _characterName.text = character.DisplayName.ToUpperInvariant();
        _characterCounter.text = $"{_selectedIndex + 1} / {_characters.Count}";
        _description.text = !character.IsConfigured
            ? character.ConfigurationError.ToUpperInvariant()
            : string.IsNullOrWhiteSpace(character.Description)
                ? "NO DESCRIPTION"
                : character.Description.ToUpperInvariant();

        _startButton.interactable = _isInteractable && character.IsConfigured;

        CharacterSettingsConfiguration settings = character.CharacterSettings;
        if (settings == null)
        {
            _healthValue.text = "--";
            _damageValue.text = "--";
            _attackSpeedValue.text = "--";
            _movementSpeedValue.text = "--";
            return;
        }

        _healthValue.text = settings.MaxHp.ToString(CultureInfo.InvariantCulture);
        _damageValue.text = FormatSignedPercent(settings.DamageInPercent);
        _attackSpeedValue.text = FormatSignedPercent(settings.AttackSpeed);
        _movementSpeedValue.text = FormatNumber(settings.MovementSpeed);
    }

    private void RequestRelativeSelection(int direction)
    {
        if (_characters == null || _characters.Count == 0)
            return;

        RequestSelection(_selectedIndex + direction);
    }

    private void RequestSelection(int index)
    {
        int wrappedIndex = WrapIndex(index);
        SelectionRequested?.Invoke(wrappedIndex);
    }

    private void FocusStart()
    {
        if (EventSystem.current == null)
            return;

        if (_startButton != null && _startButton.interactable)
            EventSystem.current.SetSelectedGameObject(_startButton.gameObject);
        else if (_backButton != null && _backButton.interactable)
            EventSystem.current.SetSelectedGameObject(_backButton.gameObject);
    }

    private void FocusBack()
    {
        if (EventSystem.current != null && _backButton != null && _backButton.interactable)
            EventSystem.current.SetSelectedGameObject(_backButton.gameObject);
    }

    private int WrapIndex(int index) =>
        (index % _characters.Count + _characters.Count) % _characters.Count;

    private static string FormatNumber(float value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string FormatSignedPercent(float value)
    {
        string prefix = value > 0f ? "+" : string.Empty;
        return $"{prefix}{FormatNumber(value)}%";
    }
}
