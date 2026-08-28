using DG.Tweening;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterPortraitSlotView : MonoBehaviour
{
    [SerializeField] private CharacterSelectionView _owner;
    [SerializeField] private Button _button;
    [SerializeField] private Image _portrait;
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private UIButtonJuice _buttonJuice;
    [SerializeField] private RectTransform _selectionFrame;
    [SerializeField] private CanvasGroup _selectionFrameCanvasGroup;
    [SerializeField] private TMP_Text _lockedLabel;

    [Header("Selection Style")]
    [SerializeField] private float _selectedScale = 1.22f;
    [SerializeField] private float _selectionScaleDuration = 0.22f;

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
    private bool _isLocked;

    public RectTransform RectTransform => _rectTransform;
    public bool IsVisible => gameObject.activeSelf;

    public void Bind(int characterIndex, int relativeDirection, CharacterDefinition character,
        Sprite portraitPlaceholder)
    {
        _isLocked = false;
        _characterIndex = characterIndex;
        _relativeDirection = relativeDirection;
        if (_portrait != null)
        {
            _portrait.material = null;
            _portrait.sprite = character.Portrait != null ? character.Portrait : portraitPlaceholder;
            _portrait.enabled = _portrait.sprite != null;
            _portrait.color = Color.white;
        }

        SetLockedLabelVisible(false);
        gameObject.SetActive(true);
    }

    public void BindLocked(Sprite portraitPlaceholder)
    {
        _isLocked = true;
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
        _portrait.color = Color.white;
    }

    public void Clear()
    {
        KillAnimations();
        _isLocked = false;
        SetLockedLabelVisible(false);
        gameObject.SetActive(false);
    }

    public void SetInteractable(bool interactable) =>
        _button.interactable = interactable && !_isLocked;

    public void SetSelected(bool selected, bool animate)
    {
        selected &= !_isLocked;
        _buttonJuice.SetBaseScaleMultiplier(
            selected ? _selectedScale : 1f,
            animate,
            _selectionScaleDuration,
            Ease.OutCubic);

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
        _buttonJuice.SetBaseScaleMultiplier(1f, false, 0f, Ease.OutCubic);
        ResetSelectionFrame();
    }

    private void OnDisable() =>
        KillAnimations();
}
