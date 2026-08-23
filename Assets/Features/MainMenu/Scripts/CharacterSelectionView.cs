using System;
using System.Collections.Generic;
using System.Globalization;
using Cysharp.Threading.Tasks;
using Features.Relics.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class CharacterSelectionView : MonoBehaviour
{
    [Header("Roster")]
    [SerializeField] private Sprite _portraitPlaceholder;
    [SerializeField] private CharacterPortraitSlotView[] _portraitSlots;

    [Header("Character")]
    [SerializeField] private TMP_Text _characterName;
    [SerializeField] private TMP_Text _description;

    [Header("Stats")]
    [SerializeField] private TMP_Text _healthValue;
    [SerializeField] private TMP_Text _damageValue;
    [SerializeField] private TMP_Text _attackSpeedValue;
    [SerializeField] private TMP_Text _movementSpeedValue;

    [Header("Preview")]
    [SerializeField] private CharacterPreviewRenderer _previewRenderer;
    [SerializeField] private RawImage _previewViewport;

    [Header("Loadout")]
    [SerializeField] private Image _abilityIcon;
    [SerializeField] private TMP_Text _abilityName;
    [SerializeField] private TMP_Text _abilityDescription;
    [SerializeField] private Image _abilityAccent;
    [SerializeField] private Image _relicIcon;
    [SerializeField] private TMP_Text _relicName;
    [SerializeField] private TMP_Text _relicDescription;
    [SerializeField] private Image _relicAccent;

    [Header("Navigation")]
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _backButton;

    private IReadOnlyList<CharacterDefinition> _characters;
    private int _selectedIndex;
    private bool _isInteractable;

    public event Action<int, int> SelectionRequested;
    public event Action StartRequested;
    public event Action BackRequested;

    private void Awake()
    {
        if (_previewRenderer == null || _previewViewport == null)
        {
            throw new InvalidOperationException(
                "Character selection prefab requires a preview renderer and viewport.");
        }

        _previewRenderer.Initialize(_previewViewport);
        _previewRenderer.PortraitRendered += HandlePortraitRendered;
    }

    public void Show(IReadOnlyList<CharacterDefinition> characters, int selectedIndex)
    {
        if (characters == null || characters.Count == 0)
            throw new InvalidOperationException("Character selection requires at least one configured character.");

        if (_portraitSlots == null || _portraitSlots.Length == 0)
            throw new InvalidOperationException("Character selection prefab requires portrait slots.");

        _characters = characters;
        gameObject.SetActive(true);
        SetSelectedIndex(selectedIndex);
        SetInteractable(true);
        _previewRenderer?.PrewarmPortraitsAsync(characters, destroyCancellationToken).Forget();
        FocusStart();
    }

    public void Hide()
    {
        _previewRenderer?.ClearPreview();
        gameObject.SetActive(false);
    }

    public void SetSelectedIndex(int index, int direction = 0)
    {
        if (_characters == null || _characters.Count == 0)
            return;

        int targetIndex = WrapIndex(index);
        bool animateSelection = gameObject.activeInHierarchy && targetIndex != _selectedIndex;
        _selectedIndex = targetIndex;
        RefreshPortraitSlots(animateSelection);
        RefreshCharacterDetails();
        ApplyInteractableState();
    }

    public void SetInteractable(bool interactable)
    {
        _isInteractable = interactable;
        ApplyInteractableState();
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

    public void RequestStart()
    {
        if (_isInteractable)
            StartRequested?.Invoke();
    }

    public void RequestBack()
    {
        if (_isInteractable)
            BackRequested?.Invoke();
    }

    internal void RequestSelectionFromSlot(int index, int relativeDirection)
    {
        int direction = relativeDirection != 0
            ? Math.Sign(relativeDirection)
            : index < _selectedIndex ? -1 : 1;
        RequestSelection(index, direction);
    }

    private void RefreshPortraitSlots(bool animateSelection)
    {
        int firstCharacterIndex = GetFirstVisibleCharacterIndex();
        for (int slotIndex = 0; slotIndex < _portraitSlots.Length; slotIndex++)
        {
            CharacterPortraitSlotView slot = _portraitSlots[slotIndex];
            int characterIndex = firstCharacterIndex + slotIndex;
            if (characterIndex >= _characters.Count)
            {
                slot.BindLocked(_portraitPlaceholder);
                continue;
            }

            CharacterDefinition character = _characters[characterIndex];
            slot.Bind(characterIndex, characterIndex - _selectedIndex, character,
                _portraitPlaceholder);
            if (_previewRenderer != null &&
                _previewRenderer.TryGetPortrait(character.Id, out Sprite renderedPortrait))
            {
                slot.SetPortrait(renderedPortrait);
            }

            slot.SetSelected(characterIndex == _selectedIndex, animateSelection);
            slot.SetInteractable(_isInteractable);
        }
    }

    private int GetFirstVisibleCharacterIndex()
    {
        if (_characters.Count <= _portraitSlots.Length)
            return 0;

        int halfWindow = _portraitSlots.Length / 2;
        return Mathf.Clamp(_selectedIndex - halfWindow, 0,
            _characters.Count - _portraitSlots.Length);
    }

    private void RefreshCharacterDetails()
    {
        CharacterDefinition character = _characters[_selectedIndex];
        _previewRenderer?.ShowCharacterAsync(character, destroyCancellationToken).Forget();

        _characterName.text = character.DisplayName.ToUpperInvariant();
        _description.text = !character.IsConfigured
            ? character.ConfigurationError.ToUpperInvariant()
            : string.IsNullOrWhiteSpace(character.Description)
                ? "NO DESCRIPTION"
                : character.Description.ToUpperInvariant();

        RefreshStats(character.CharacterSettings);
        RefreshLoadout(character);
    }

    private void RefreshStats(CharacterSettingsConfiguration settings)
    {
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

    private void RefreshLoadout(CharacterDefinition character)
    {
        AbilityConfiguration ability = character.StartingAbilityDetails;
        SetOptionalIcon(_abilityIcon, ability != null ? ability.Icon : null);
        _abilityName.text = ability != null && !string.IsNullOrWhiteSpace(ability.DisplayName)
            ? ability.DisplayName.ToUpperInvariant()
            : character.StartingAbility.ToString().ToUpperInvariant();
        _abilityDescription.text = ability != null && !string.IsNullOrWhiteSpace(ability.Description)
            ? ability.Description.ToUpperInvariant()
            : "NO ABILITY DESCRIPTION";
        SetOptionalColor(_abilityAccent, new Color(1f, 0.46f, 0.16f));

        RelicDefinition relic = character.StartingRelic;
        if (relic == null)
        {
            SetOptionalIcon(_relicIcon, null);
            _relicName.text = "EMPTY RELIC SLOT";
            _relicDescription.text = "THIS CHARACTER DOES NOT START WITH A RELIC.";
            SetOptionalColor(_relicAccent, new Color(0.38f, 0.34f, 0.4f));
            return;
        }

        SetOptionalIcon(_relicIcon, relic.Icon);
        _relicName.text = string.IsNullOrWhiteSpace(relic.DisplayName)
            ? relic.Id.ToUpperInvariant()
            : relic.DisplayName.ToUpperInvariant();
        _relicDescription.text = string.IsNullOrWhiteSpace(relic.Description)
            ? "NO RELIC DESCRIPTION"
            : relic.Description.ToUpperInvariant();
        SetOptionalColor(_relicAccent, RelicRarityPalette.GetColor(relic.Rarity));
    }

    private static void SetOptionalIcon(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    private static void SetOptionalColor(Image image, Color color)
    {
        if (image != null)
            image.color = color;
    }

    private void HandlePortraitRendered(string characterId, Sprite portrait)
    {
        if (!gameObject.activeInHierarchy || _characters == null || portrait == null)
            return;

        RefreshPortraitSlots(false);
        ApplyInteractableState();
    }

    private void ApplyInteractableState()
    {
        bool hasCharacters = _characters is { Count: > 0 };
        _startButton.interactable = _isInteractable && hasCharacters &&
            _characters[_selectedIndex].IsConfigured;
        _backButton.interactable = _isInteractable;

        if (_portraitSlots == null)
            return;

        foreach (CharacterPortraitSlotView slot in _portraitSlots)
            slot.SetInteractable(_isInteractable && slot.IsVisible);
    }

    private void RequestRelativeSelection(int direction)
    {
        if (!_isInteractable || _characters == null || _characters.Count < 2)
            return;

        RequestSelection(_selectedIndex + direction, direction);
    }

    private void RequestSelection(int index, int direction)
    {
        if (!_isInteractable || _characters == null || _characters.Count < 2)
            return;

        int wrappedIndex = WrapIndex(index);
        if (wrappedIndex == _selectedIndex)
            return;

        SelectionRequested?.Invoke(wrappedIndex, direction < 0 ? -1 : 1);
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

    private void OnDisable() =>
        _previewRenderer?.ClearPreview();

    private void OnDestroy()
    {
        if (_previewRenderer != null)
            _previewRenderer.PortraitRendered -= HandlePortraitRendered;
    }
}
