using System;
using DG.Tweening;
using Features.Relics.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Features.Quests.Scripts
{
    public sealed class UnlockCellView : MonoBehaviour, ISelectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private UnityEngine.UI.Button _button;
        [SerializeField] private UnityEngine.UI.Image _background;
        [SerializeField] private UnityEngine.UI.Image _frame;
        [SerializeField] private UnityEngine.UI.Image _icon;
        [SerializeField] private RectTransform _priceRoot;
        [SerializeField] private TMP_Text _priceLabel;
        [SerializeField] private UnityEngine.UI.Image _alert;
        [SerializeField] private RectTransform _selectionFrame;
        [SerializeField] private CanvasGroup _selectionFrameCanvasGroup;
        [SerializeField] private Sprite _neutralBackgroundSprite;
        [SerializeField] private Sprite _commonRelicBackgroundSprite;
        [SerializeField] private Sprite _uncommonRelicBackgroundSprite;
        [SerializeField] private Sprite _rareRelicBackgroundSprite;
        [SerializeField] private Sprite _legendaryRelicBackgroundSprite;
        [SerializeField] private Color _frameColor = Color.white;
        [SerializeField, Range(0f, 1f)] private float _lockedBrightness = 0.4f;
        [SerializeField] private Color _purchaseIconColor = new(0.29f, 0.29f, 0.29f);

        [Header("Selection Style")]
        [SerializeField] private float _selectedIconScale = 1.15f;
        [SerializeField] private float _iconSelectionScaleDuration = 0.22f;
        [SerializeField] private float _iconPumpScale = 1.12f;
        [SerializeField] private float _iconPumpDuration = 0.24f;

        [Header("Selection Pulse")]
        [SerializeField] private float _appearanceDuration = 0.14f;
        [SerializeField] private float _pulseDuration = 0.6f;
        [SerializeField] private float _pulseScale = 1.045f;
        [SerializeField] private float _pulseMinAlpha = 0.58f;
        [SerializeField] private float _pulseMaxAlpha = 1f;

        private Action _selected;
        private Tween _selectionFadeTween;
        private Tween _selectionPulseScaleTween;
        private Tween _selectionPulseAlphaTween;
        private Tween _iconPumpTween;
        private Vector3 _iconBaseScale;
        private bool _hasIconBaseScale;
        private bool _hasSelectionState;
        private bool _isSelected;
        private bool _isOwned;
        private bool _requirementMet;
        private bool _isHovered;

        public UnlockDefinition Unlock { get; private set; }
        public UnityEngine.UI.Button Button => _button;
        public UnityEngine.UI.Image Icon => _icon;

        public void Bind(UnlockDefinition unlock, Action selected)
        {
            Unlock = unlock;
            _selected = selected;
            _isOwned = false;
            _requirementMet = false;
            _isHovered = false;
            _priceLabel.text = unlock.SilverCost.ToString("N0");
            _button.onClick.RemoveListener(Select);
            _button.onClick.AddListener(Select);
            ApplyBackground();
        }

        public void RefreshState(bool owned, bool requirementMet, bool isNew, bool showPrice = true)
        {
            _isOwned = owned;
            _requirementMet = requirementMet;
            ApplyBackground();
            if (!owned && requirementMet)
                _icon.color = _purchaseIconColor;
            _priceRoot.gameObject.SetActive(!owned && requirementMet && showPrice);
            _alert.gameObject.SetActive(isNew);
        }

        public void SetSelected(bool selected, bool animate)
        {
            if (_hasSelectionState && _isSelected == selected)
                return;

            _hasSelectionState = true;
            _isSelected = selected;
            ApplyBackground();

            SetIconScale(selected, animate);

            if (selected)
                ShowSelectionFrame(animate);
            else
                HideSelectionFrame(animate);
        }

        public void OnSelect(BaseEventData eventData) => Select();

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_button.IsInteractable())
                return;

            _isHovered = true;
            ApplyBackground();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            ApplyBackground();
        }

        private void Select() => _selected?.Invoke();

        private void ApplyBackground()
        {
            Sprite backgroundSprite = GetBackgroundSprite();
            Color color = Color.white;
            if (!_isOwned && !_requirementMet)
                color *= _lockedBrightness;
            color.a = 1f;

            _background.sprite = backgroundSprite;
            _background.color = color;
            _frame.sprite = backgroundSprite;
            _frame.color = (_isHovered ? Color.white : _frameColor) * color;
        }

        private Sprite GetBackgroundSprite()
        {
            if (Unlock?.Relic == null)
                return _neutralBackgroundSprite;

            return Unlock.Relic.Rarity switch
            {
                RelicRarity.Common => _commonRelicBackgroundSprite,
                RelicRarity.Uncommon => _uncommonRelicBackgroundSprite,
                RelicRarity.Rare => _rareRelicBackgroundSprite,
                RelicRarity.Legendary => _legendaryRelicBackgroundSprite,
                _ => _neutralBackgroundSprite
            };
        }

        private void SetIconScale(bool selected, bool animate)
        {
            KillIconPump(false);
            if (_icon == null)
                return;

            RectTransform iconTransform = _icon.rectTransform;
            Vector3 targetScale = _iconBaseScale * (selected ? _selectedIconScale : 1f);
            if (!animate || !gameObject.activeInHierarchy)
            {
                iconTransform.localScale = targetScale;
                return;
            }

            if (!selected)
            {
                _iconPumpTween = iconTransform.DOScale(targetScale, _iconSelectionScaleDuration)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true)
                    .SetLink(gameObject);
                return;
            }

            _iconPumpTween = DOTween.Sequence()
                .Append(iconTransform.DOScale(targetScale * _iconPumpScale,
                    _iconPumpDuration * 0.32f).SetEase(Ease.OutCubic))
                .Append(iconTransform.DOScale(targetScale,
                    _iconPumpDuration * 0.68f).SetEase(Ease.OutBack))
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void KillIconPump(bool resetScale = true)
        {
            _iconPumpTween?.Kill();
            _iconPumpTween = null;
            if (_icon == null)
                return;

            if (!_hasIconBaseScale)
            {
                _iconBaseScale = _icon.rectTransform.localScale;
                _hasIconBaseScale = true;
            }

            if (resetScale)
                _icon.rectTransform.localScale = _iconBaseScale;
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

        private void OnDisable()
        {
            KillSelectionFrameTweens();
            KillIconPump();
            _hasSelectionState = false;
            _isSelected = false;
            _isHovered = false;
            ApplyBackground();
            ResetSelectionFrame();
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(Select);
        }
    }
}
