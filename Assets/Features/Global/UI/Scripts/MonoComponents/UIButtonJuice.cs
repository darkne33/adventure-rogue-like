using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    [DisallowMultipleComponent]
    public sealed class UIButtonJuice : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        ISubmitHandler
    {
        [Header("Target")]
        [SerializeField] private Transform _scaleTarget;

        [Header("Scale")]
        [SerializeField, Min(1f)] private float _hoverScale = 1.06f;
        [SerializeField, Range(0f, 1f)] private float _pressedScale = 0.92f;
        [SerializeField, Min(1f)] private float _pumpScale = 1.10f;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float _hoverDuration = 0.12f;
        [SerializeField, Min(0f)] private float _pressDuration = 0.08f;
        [SerializeField, Min(0f)] private float _pumpDuration = 0.20f;

        [Header("Easing")]
        [SerializeField] private Ease _hoverEase = Ease.OutBack;
        [SerializeField] private Ease _pressEase = Ease.OutQuad;
        [SerializeField] private Ease _pumpEase = Ease.OutBack;
        [SerializeField] private bool _useUnscaledTime = true;

        private Selectable _selectable;
        private Vector3 _initialScale;
        private Vector3 _defaultScale;
        private Tween _scaleTween;
        private bool _isHovered;
        private bool _isPressed;
        private bool _isInitialized;

        private bool CanAnimate =>
            _selectable == null || (_selectable.interactable && _selectable.IsInteractable());

        private void Awake() =>
            Initialize();

        public void SetBaseScaleMultiplier(float multiplier, bool animate, float duration, Ease ease)
        {
            Initialize();
            _defaultScale = _initialScale * Mathf.Max(0f, multiplier);

            Vector3 targetScale = _isPressed
                ? _defaultScale * _pressedScale
                : GetRestingScale();

            if (!animate || !gameObject.activeInHierarchy)
            {
                KillScaleTween();
                _scaleTarget.localScale = targetScale;
                return;
            }

            TweenTo(targetScale, duration, ease);
        }

        private void OnDisable()
        {
            _isHovered = false;
            _isPressed = false;
            KillScaleTween();

            if (_isInitialized && _scaleTarget != null)
            {
                _defaultScale = _initialScale;
                _scaleTarget.localScale = _initialScale;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;

            if (!CanAnimate)
            {
                RestoreImmediately();
                return;
            }

            if (!_isPressed)
                TweenTo(_defaultScale * _hoverScale, _hoverDuration, _hoverEase);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;

            if (!CanAnimate)
            {
                RestoreImmediately();
                return;
            }

            if (!_isPressed)
                TweenTo(_defaultScale, _hoverDuration, _hoverEase);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanAnimate)
            {
                _isPressed = false;
                RestoreImmediately();
                return;
            }

            _isPressed = true;
            TweenTo(_defaultScale * _pressedScale, _pressDuration, _pressEase);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isPressed)
                return;

            _isPressed = false;

            if (!CanAnimate)
            {
                RestoreImmediately();
                return;
            }

            PlayPump();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!CanAnimate)
            {
                RestoreImmediately();
                return;
            }

            _isPressed = false;
            PlayPump();
        }

        private void PlayPump()
        {
            KillScaleTween();

            Vector3 restingScale = GetRestingScale();
            if (_pumpDuration <= 0f)
            {
                _scaleTarget.localScale = restingScale;
                return;
            }

            float halfDuration = _pumpDuration * 0.5f;
            _scaleTween = DOTween.Sequence()
                .Append(_scaleTarget
                    .DOScale(_defaultScale * _pumpScale, halfDuration)
                    .SetEase(_pumpEase))
                .Append(_scaleTarget
                    .DOScale(restingScale, halfDuration)
                    .SetEase(_hoverEase))
                .SetUpdate(_useUnscaledTime)
                .SetLink(gameObject);
        }

        private Vector3 GetRestingScale()
        {
            return _isHovered ? _defaultScale * _hoverScale : _defaultScale;
        }

        private void TweenTo(Vector3 scale, float duration, Ease ease)
        {
            KillScaleTween();

            if (duration <= 0f)
            {
                _scaleTarget.localScale = scale;
                return;
            }

            _scaleTween = _scaleTarget
                .DOScale(scale, duration)
                .SetEase(ease)
                .SetUpdate(_useUnscaledTime)
                .SetLink(gameObject);
        }

        private void RestoreImmediately()
        {
            KillScaleTween();

            if (_scaleTarget != null)
                _scaleTarget.localScale = _defaultScale;
        }

        private void KillScaleTween()
        {
            _scaleTween?.Kill();
            _scaleTween = null;
        }

        private void Initialize()
        {
            if (_isInitialized)
                return;

            _scaleTarget ??= transform;
            _selectable = GetComponent<Selectable>();
            _initialScale = _scaleTarget.localScale;
            _defaultScale = _initialScale;
            _isInitialized = true;
        }
    }
}
