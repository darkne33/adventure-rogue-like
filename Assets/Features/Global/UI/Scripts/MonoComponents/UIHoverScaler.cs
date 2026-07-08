using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    [DisallowMultipleComponent]
    public class UIHoverScaler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Transform _scaleTarget;
        [SerializeField, Min(1f)] private float _hoverScale = 1.1f;
        [SerializeField, Min(0f)] private float _scaleDuration = 0.1f;
        [SerializeField] private Ease _ease = Ease.OutQuad;
        [SerializeField] private bool _useUnscaledTime = true;

        private Vector3 _defaultScale;
        private Tween _scaleTween;

        private void Awake()
        {
            _scaleTarget ??= transform;
            _defaultScale = _scaleTarget.localScale;
        }

        private void OnDisable()
        {
            if (_scaleTarget == null) return;

            _scaleTween?.Kill();
            _scaleTarget.localScale = _defaultScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ScaleTo(_defaultScale * _hoverScale);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ScaleTo(_defaultScale);
        }

        private void ScaleTo(Vector3 scale)
        {
            _scaleTween?.Kill();

            if (_scaleDuration <= 0f)
            {
                _scaleTarget.localScale = scale;
                return;
            }

            _scaleTween = _scaleTarget
                .DOScale(scale, _scaleDuration)
                .SetEase(_ease)
                .SetUpdate(_useUnscaledTime)
                .SetLink(_scaleTarget.gameObject);
        }
    }
}
