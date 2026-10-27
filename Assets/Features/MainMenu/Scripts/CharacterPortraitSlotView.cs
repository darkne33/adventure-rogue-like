using DG.Tweening;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class CharacterPortraitSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private CharacterSelectionView _owner;
    [SerializeField] private Button _button;
    [SerializeField] private Image _portrait;
    [SerializeField] private Image _background;
    [SerializeField] private UnityEngine.UI.Image _backgroundFrame;
    [SerializeField] private Sprite _normalBackgroundSprite;
    [SerializeField] private Color _backgroundColor = new(0.62f, 0.65f, 0.72f);
    [SerializeField] private Color _lockedBackgroundColor = new(0.28f, 0.29f, 0.32f);
    [SerializeField] private Color _normalFrameColor = new(0.78f, 0.8f, 0.84f);
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private UIButtonJuice _buttonJuice;
    [SerializeField] private RectTransform _selectionFrame;
    [SerializeField] private CanvasGroup _selectionFrameCanvasGroup;
    [SerializeField] private TMP_Text _lockedLabel;

    [Header("Selection Style")]
    [SerializeField] private float _selectedScale = 1.22f;
    [SerializeField] private float _selectionScaleDuration = 0.22f;
    [SerializeField] private float _selectedPortraitScale = 1.15f;
    [SerializeField] private float _portraitPumpScale = 1.12f;
    [SerializeField] private float _portraitPumpDuration = 0.24f;

    [Header("Selection Pulse")]
    [SerializeField] private float _appearanceDuration = 0.14f;
    [SerializeField] private float _pulseDuration = 0.6f;
    [SerializeField] private float _pulseScale = 1.045f;
    [SerializeField] private float _pulseMinAlpha = 0.58f;
    [SerializeField] private float _pulseMaxAlpha = 1f;

    private int _characterIndex;
    private int _relativeDirection;
    private Tween _selectionFadeTween;
    private Tween _selectionPulseScaleTween;
    private Tween _selectionPulseAlphaTween;
    private Tween _portraitPumpTween;
    private Vector3 _portraitBaseScale;
    private bool _hasPortraitBaseScale;
    private bool _hasSelectionState;
    private bool _isSelected;
    private int _selectedCharacterIndex = -1;
    private bool _isLocked;
    private bool _isOwned = true;
    private bool _isHovered;

    public RectTransform RectTransform => _rectTransform;
    public bool IsVisible => gameObject.activeSelf;

    public void Bind(int characterIndex, int relativeDirection, CharacterDefinition character,
        Sprite portraitPlaceholder, bool isOwned = true)
    {
        _isLocked = false;
        _isOwned = isOwned;
        _characterIndex = characterIndex;
        _relativeDirection = relativeDirection;
        if (_portrait != null)
        {
            _portrait.material = null;
            _portrait.sprite = character.Portrait != null ? character.Portrait : portraitPlaceholder;
            _portrait.enabled = _portrait.sprite != null;
            _portrait.color = GetPortraitColor();
        }

        SetLockedLabelVisible(!isOwned);
        ApplyBackground();
        gameObject.SetActive(true);
    }

    public void BindLocked(Sprite portraitPlaceholder)
    {
        _isLocked = true;
        _isOwned = false;
        _characterIndex = -1;
        _relativeDirection = 0;
        if (_portrait != null)
        {
            _portrait.material = null;
            _portrait.sprite = portraitPlaceholder;
            _portrait.enabled = portraitPlaceholder != null;
            _portrait.color = new Color(0.08f, 0.08f, 0.08f, 0.72f);
        }

        gameObject.SetActive(true);
        SetSelected(false, false);
        SetLockedLabelVisible(true);
        _button.interactable = false;
    }

    public void SetPortrait(Sprite portrait, Material colorCorrectionMaterial)
    {
        if (_isLocked || portrait == null || _portrait == null)
            return;

        _portrait.material = colorCorrectionMaterial;
        _portrait.sprite = portrait;
        _portrait.enabled = true;
        _portrait.color = GetPortraitColor();
    }

    public void Clear()
    {
        KillAnimations();
        _isLocked = false;
        _isOwned = true;
        SetLockedLabelVisible(false);
        gameObject.SetActive(false);
    }

    public void SetInteractable(bool interactable)
    {
        _button.interactable = interactable && !_isLocked;
        if (!_button.interactable)
            _isHovered = false;
        ApplyFrameColor();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = _button.IsInteractable();
        ApplyFrameColor();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        ApplyFrameColor();
    }

    public void SetSelected(bool selected, bool animate)
    {
        selected &= !_isLocked;
        bool selectionChanged = !_hasSelectionState || _isSelected != selected ||
            (selected && _selectedCharacterIndex != _characterIndex);
        _hasSelectionState = true;
        _isSelected = selected;
        _selectedCharacterIndex = selected ? _characterIndex : -1;
        ApplyBackground();

        if (!selectionChanged)
            return;

        _buttonJuice.SetBaseScaleMultiplier(
            selected ? _selectedScale : 1f,
            animate,
            _selectionScaleDuration,
            selected ? Ease.OutBack : Ease.OutCubic);

        SetPortraitScale(selected, animate);

        if (selected)
            ShowSelectionFrame(animate);
        else
            HideSelectionFrame(animate);
    }

    public void RequestSelection()
    {
        if (!_isLocked)
            _owner.RequestSelectionFromSlot(_characterIndex, _relativeDirection);
    }

    private void SetLockedLabelVisible(bool visible)
    {
        if (_lockedLabel != null)
            _lockedLabel.gameObject.SetActive(visible);
    }

    private Color GetPortraitColor() =>
        _isOwned ? Color.white : new Color(0.42f, 0.42f, 0.48f, 1f);

    private void ApplyBackground()
    {
        if (_background != null)
        {
            _background.sprite = _normalBackgroundSprite;
            _background.color = _isLocked || !_isOwned ? _lockedBackgroundColor : _backgroundColor;
        }

        ApplyFrameColor();
    }

    private void ApplyFrameColor()
    {
        if (_backgroundFrame != null)
            _backgroundFrame.color = _isHovered ? Color.white : _normalFrameColor;
    }

    private void SetPortraitScale(bool selected, bool animate)
    {
        KillPortraitPump(false);
        if (_portrait == null)
            return;

        RectTransform portraitTransform = _portrait.rectTransform;
        Vector3 targetScale = _portraitBaseScale * (selected ? _selectedPortraitScale : 1f);
        if (!animate || !gameObject.activeInHierarchy)
        {
            portraitTransform.localScale = targetScale;
            return;
        }

        if (!selected)
        {
            _portraitPumpTween = portraitTransform.DOScale(targetScale, _selectionScaleDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetLink(gameObject);
            return;
        }

        _portraitPumpTween = DOTween.Sequence()
            .Append(portraitTransform.DOScale(targetScale * _portraitPumpScale,
                _portraitPumpDuration * 0.32f).SetEase(Ease.OutCubic))
            .Append(portraitTransform.DOScale(targetScale,
                _portraitPumpDuration * 0.68f).SetEase(Ease.OutBack))
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    private void KillPortraitPump(bool resetScale = true)
    {
        _portraitPumpTween?.Kill();
        _portraitPumpTween = null;
        if (_portrait == null)
            return;

        if (!_hasPortraitBaseScale)
        {
            _portraitBaseScale = _portrait.rectTransform.localScale;
            _hasPortraitBaseScale = true;
        }

        if (resetScale)
            _portrait.rectTransform.localScale = _portraitBaseScale;
    }

    private void ShowSelectionFrame(bool animate)
    {
        KillSelectionFrameTweens();
        _selectionFrame.gameObject.SetActive(true);
        _selectionFrame.localScale = Vector3.one;

        if (!gameObject.activeInHierarchy)
        {
            _selectionFrameCanvasGroup.alpha = _pulseMaxAlpha;
            return;
        }

        if (!animate)
        {
            _selectionFrameCanvasGroup.alpha = _pulseMaxAlpha;
            StartSelectionPulse();
            return;
        }

        _selectionFrameCanvasGroup.alpha = 0f;
        _selectionFadeTween = _selectionFrameCanvasGroup.DOFade(_pulseMaxAlpha, _appearanceDuration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .SetLink(gameObject)
            .OnComplete(StartSelectionPulse);
    }

    private void HideSelectionFrame(bool animate)
    {
        KillSelectionFrameTweens();

        if (!_selectionFrame.gameObject.activeSelf)
            return;

        if (!animate || !gameObject.activeInHierarchy)
        {
            ResetSelectionFrame();
            return;
        }

        _selectionFadeTween = _selectionFrameCanvasGroup.DOFade(0f, _appearanceDuration)
            .SetEase(Ease.InCubic)
            .SetUpdate(true)
            .SetLink(gameObject)
            .OnComplete(ResetSelectionFrame);
    }

    private void StartSelectionPulse()
    {
        _selectionPulseScaleTween = _selectionFrame.DOScale(Vector3.one * _pulseScale, _pulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true)
            .SetLink(gameObject);
        _selectionPulseAlphaTween = _selectionFrameCanvasGroup.DOFade(_pulseMinAlpha, _pulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    private void KillSelectionFrameTweens()
    {
        _selectionFadeTween?.Kill();
        _selectionPulseScaleTween?.Kill();
        _selectionPulseAlphaTween?.Kill();
        _selectionFadeTween = null;
        _selectionPulseScaleTween = null;
        _selectionPulseAlphaTween = null;
    }

    private void ResetSelectionFrame()
    {
        _selectionFrameCanvasGroup.alpha = 0f;
        _selectionFrame.localScale = Vector3.one;
        _selectionFrame.gameObject.SetActive(false);
    }

    private void KillAnimations()
    {
        KillSelectionFrameTweens();
        KillPortraitPump();
        _hasSelectionState = false;
        _isSelected = false;
        _selectedCharacterIndex = -1;
        ApplyBackground();
        _buttonJuice.SetBaseScaleMultiplier(1f, false, 0f, Ease.OutCubic);
        ResetSelectionFrame();
    }

    private void OnDisable()
    {
        _isHovered = false;
        KillAnimations();
    }
}
