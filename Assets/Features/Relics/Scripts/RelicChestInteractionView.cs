using System;
using DG.Tweening;
using UnityEngine;

namespace Features.Relics.Scripts
{
    [Serializable]
    public sealed class RelicChestInteractionView
    {
        [SerializeField] private Outline _outline;
        [SerializeField] private CanvasGroup _promptCanvasGroup;
        [SerializeField] private Transform _promptTransform;
        [SerializeField, Min(0f)] private float _showDuration = 0.14f;
        [SerializeField, Min(0f)] private float _hideDuration = 0.12f;

        private GameObject _owner;
        private bool _isAvailable;
        private Vector3 _visibleScale = Vector3.one;
        private Vector3 _hiddenScale = Vector3.one * 0.82f;

        public void Initialize(GameObject owner)
        {
            _owner = owner;

            if (_promptTransform != null)
            {
                _visibleScale = _promptTransform.localScale;
                _hiddenScale = _visibleScale * 0.82f;
            }

            SetAvailable(false, true);
        }

        public void SetAvailable(bool isAvailable, bool instantly = false)
        {
            if (_isAvailable == isAvailable && instantly == false)
                return;

            _isAvailable = isAvailable;

            if (_outline != null)
                _outline.enabled = isAvailable;

            if (_promptCanvasGroup == null || _promptTransform == null)
                return;

            _promptCanvasGroup.DOKill();
            _promptTransform.DOKill();

            if (instantly)
            {
                _promptCanvasGroup.alpha = isAvailable ? 1f : 0f;
                _promptTransform.localScale = isAvailable ? _visibleScale : _hiddenScale;
                return;
            }

            float duration = isAvailable ? _showDuration : _hideDuration;
            _ = _promptCanvasGroup.DOFade(isAvailable ? 1f : 0f, duration)
                .SetEase(isAvailable ? Ease.OutQuad : Ease.InQuad)
                .SetLink(_owner);
            _ = _promptTransform.DOScale(isAvailable ? _visibleScale : _hiddenScale, duration)
                .SetEase(isAvailable ? Ease.OutBack : Ease.InQuad)
                .SetLink(_owner);
        }
    }
}
