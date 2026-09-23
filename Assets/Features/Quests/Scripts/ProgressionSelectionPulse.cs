using DG.Tweening;
using UnityEngine;

namespace Features.Quests.Scripts
{
    [DisallowMultipleComponent]
    public sealed class ProgressionSelectionPulse : MonoBehaviour
    {
        [SerializeField] private RectTransform _frame;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _appearanceDuration = 0.14f;
        [SerializeField] private float _pulseDuration = 0.6f;
        [SerializeField] private float _pulseScale = 1.045f;
        [SerializeField] private float _pulseMinAlpha = 0.58f;
        [SerializeField] private float _pulseMaxAlpha = 1f;

        private Tween _fadeTween;
        private Tween _pulseScaleTween;
        private Tween _pulseAlphaTween;

        private void OnEnable()
        {
            StopAndReset();
            if (_frame == null || _canvasGroup == null)
                return;

            _fadeTween = _canvasGroup.DOFade(_pulseMaxAlpha, _appearanceDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(StartPulse);
        }

        private void StartPulse()
        {
            _pulseScaleTween = _frame.DOScale(Vector3.one * _pulseScale, _pulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetLink(gameObject);
            _pulseAlphaTween = _canvasGroup.DOFade(_pulseMinAlpha, _pulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void StopAndReset()
        {
            _fadeTween?.Kill();
            _pulseScaleTween?.Kill();
            _pulseAlphaTween?.Kill();
            _fadeTween = null;
            _pulseScaleTween = null;
            _pulseAlphaTween = null;

            if (_frame != null)
                _frame.localScale = Vector3.one;
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;
        }

        private void OnDisable() => StopAndReset();

        private void OnDestroy() => StopAndReset();
    }
}
