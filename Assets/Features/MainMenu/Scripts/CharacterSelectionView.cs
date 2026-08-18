using System;
using System.Collections.Generic;
using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class CharacterSelectionView : MonoBehaviour
{
    private const int CarouselCenterSlotIndex = 2;
    private const int CarouselSlotCount = 5;

    [Header("Roster")]
    [SerializeField] private Sprite _portraitPlaceholder;
    [SerializeField] private RectTransform _portraitStrip;
    [SerializeField] private CharacterPortraitSlotView[] _portraitSlots;

    [Header("Carousel")]
    [SerializeField] private float _carouselSpacing = 240f;
    [SerializeField] private float _carouselDuration = 0.22f;

    [Header("Character")]
    [SerializeField] private Image _selectedPortrait;
    [SerializeField] private TMP_Text _characterName;
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
    private bool _isAnimating;
    private Sequence _carouselTween;

    public event Action<int, int> SelectionRequested;
    public event Action StartRequested;
    public event Action BackRequested;

    public void Show(IReadOnlyList<CharacterDefinition> characters, int selectedIndex)
    {
        if (characters == null || characters.Count == 0)
            throw new InvalidOperationException("Character selection requires at least one configured character.");

        if (_portraitSlots == null || _portraitSlots.Length < CarouselSlotCount)
            throw new InvalidOperationException(
                $"Character selection prefab requires at least {CarouselSlotCount} carousel slots.");

        _characters = characters;
        gameObject.SetActive(true);
        SetSelectedIndex(selectedIndex);
        SetInteractable(true);
        FocusStart();
    }

    public void Hide()
    {
        StopCarouselAnimation();
        gameObject.SetActive(false);
    }

    public void SetSelectedIndex(int index, int direction = 0)
    {
        if (_characters == null || _characters.Count == 0)
            return;

        int targetIndex = WrapIndex(index);
        if (direction == 0 || !gameObject.activeInHierarchy || targetIndex == _selectedIndex)
        {
            _selectedIndex = targetIndex;
            RefreshPortraitSlots();
            RefreshCharacterDetails();
            return;
        }

        AnimateCarousel(targetIndex, ResolveTransitionDirection(targetIndex, direction));
    }

    private void ApplySelectedIndexImmediately(int index)
    {
        _selectedIndex = WrapIndex(index);
        RefreshPortraitSlots();
        RefreshCharacterDetails();
    }

    public void SetInteractable(bool interactable)
    {
        _isInteractable = interactable;
        ApplyInteractableState();
    }

    private void ApplyInteractableState()
    {
        bool interactable = _isInteractable;
        bool canChangeCharacter = interactable && _characters is { Count: > 1 };
        _previousButton.interactable = canChangeCharacter;
        _nextButton.interactable = canChangeCharacter;
        _startButton.interactable = interactable && _characters is { Count: > 0 } &&
            _characters[_selectedIndex].IsConfigured;
        _backButton.interactable = interactable;

        foreach (CharacterPortraitSlotView slot in _portraitSlots)
            slot.SetInteractable(interactable && slot.IsVisible);
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
        if (!_isInteractable || _isAnimating)
            return;

        StartRequested?.Invoke();
    }

    public void RequestBack()
    {
        if (!_isInteractable || _isAnimating)
            return;

        BackRequested?.Invoke();
    }

    internal void RequestSelectionFromSlot(int index, int relativeDirection) =>
        RequestSelection(index, relativeDirection);

    private void RefreshPortraitSlots()
    {
        for (int slotIndex = 0; slotIndex < _portraitSlots.Length; slotIndex++)
        {
            CharacterPortraitSlotView slot = _portraitSlots[slotIndex];
            if (slotIndex >= CarouselSlotCount)
            {
                slot.Clear();
                continue;
            }

            int relativeDirection = slotIndex - CarouselCenterSlotIndex;
            int characterIndex = GetCharacterIndexForRole(relativeDirection);
            if (characterIndex < 0)
            {
                slot.Clear();
                continue;
            }

            slot.Bind(characterIndex, relativeDirection, _characters[characterIndex],
                _portraitPlaceholder);
            slot.RectTransform.anchoredPosition =
                new Vector2(relativeDirection * _carouselSpacing, 0f);
            slot.SetSelected(relativeDirection == 0, false);
            slot.SetInteractable(_isInteractable && !_isAnimating);
        }
    }

    private int GetCharacterIndexForRole(int relativeDirection)
    {
        if (_characters.Count == 1)
            return relativeDirection == 0 ? _selectedIndex : -1;

        if (_characters.Count == 2)
        {
            if (relativeDirection == 0)
                return _selectedIndex;

            int otherCharacterIndex = _selectedIndex == 0 ? 1 : 0;
            int otherCharacterSide = _selectedIndex == 0 ? 1 : -1;
            return relativeDirection == otherCharacterSide ? otherCharacterIndex : -1;
        }

        return WrapIndex(_selectedIndex + relativeDirection);
    }

    private void RefreshCharacterDetails()
    {
        CharacterDefinition character = _characters[_selectedIndex];
        Sprite portrait = character.Portrait != null ? character.Portrait : _portraitPlaceholder;

        _selectedPortrait.sprite = portrait;
        _selectedPortrait.enabled = portrait != null;
        _characterName.text = character.DisplayName.ToUpperInvariant();
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
        if (!_isInteractable || _isAnimating || _characters == null || _characters.Count < 2)
            return;

        RequestSelection(_selectedIndex + direction, direction);
    }

    private void RequestSelection(int index, int direction)
    {
        if (!_isInteractable || _isAnimating || _characters == null || _characters.Count < 2)
            return;

        int wrappedIndex = WrapIndex(index);
        if (wrappedIndex == _selectedIndex)
            return;

        SelectionRequested?.Invoke(wrappedIndex, direction);
    }

    private int ResolveTransitionDirection(int targetIndex, int requestedDirection)
    {
        if (_characters.Count == 2)
            return _selectedIndex == 0 && targetIndex == 1 ? 1 : -1;

        return requestedDirection < 0 ? -1 : 1;
    }

    private void AnimateCarousel(int targetIndex, int direction)
    {
        if (_isAnimating)
            return;

        _isAnimating = true;
        ApplyInteractableState();

        int incomingSlotIndex = CarouselCenterSlotIndex + direction;
        for (int slotIndex = 0; slotIndex < CarouselSlotCount; slotIndex++)
        {
            CharacterPortraitSlotView slot = _portraitSlots[slotIndex];
            if (slot.IsVisible)
                slot.SetSelected(slotIndex == incomingSlotIndex, true);
        }

        _carouselTween = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject);

        for (int slotIndex = 0; slotIndex < CarouselSlotCount; slotIndex++)
        {
            CharacterPortraitSlotView slot = _portraitSlots[slotIndex];
            if (!slot.IsVisible)
                continue;

            float targetPosition = slot.RectTransform.anchoredPosition.x -
                direction * _carouselSpacing;
            _carouselTween.Join(slot.RectTransform.DOAnchorPosX(targetPosition, _carouselDuration)
                .SetEase(Ease.OutCubic));
        }

        _carouselTween.OnComplete(() =>
        {
            _carouselTween = null;
            _isAnimating = false;
            ApplySelectedIndexImmediately(targetIndex);
            ApplyInteractableState();
        });
    }

    private void StopCarouselAnimation()
    {
        _carouselTween?.Kill();
        _carouselTween = null;
        _isAnimating = false;

        if (_characters is { Count: > 0 })
            RefreshPortraitSlots();
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

    private void OnDisable()
    {
        _carouselTween?.Kill();
        _carouselTween = null;
        _isAnimating = false;
    }
}
