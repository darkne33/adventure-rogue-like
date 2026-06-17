using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace UI
{
    [Serializable]
    public class OpenPanelPumpAnimations : IPanelAnimation
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Transform _pumpRoot;

        private float _showTime = .09f;
        private float _hideTime = .1f;

        private CancellationTokenSource _cts;

        public void Initialize()
        {
        }

        public UniTask Show()
        {
            ForceShow();
            return DOTween.Sequence()
                .SetId($"{_canvasGroup.gameObject.name} Pump Show")
                .SetLink(_pumpRoot.gameObject)
                .SetUpdate(true)
                .Append(_pumpRoot.DOScale(1.03f, _showTime).From(0.97f))
                .Append(_pumpRoot.DOScale(1f, _showTime))
                .ToUniTask(cancellationToken: GetToken());
        }

        public UniTask Hide() =>
            _canvasGroup
                .DOFade(0, _hideTime)
                .SetId($"{_canvasGroup.gameObject.name} Pump Hide")
                .SetUpdate(true)
                .ToUniTask(cancellationToken: GetToken());

        public void ForceShow()
        {
            Cleanup();
            _canvasGroup.alpha = 1;
        }

        public void ForceHide()
        {
            Cleanup();
            _canvasGroup.alpha = 0;
        }

        public void Cleanup()
        {
            _cts?.Cancel();
        }

        private CancellationToken GetToken()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            return _cts.Token;
        }

#if UNITY_EDITOR
        public void SetCanvasGroup(CanvasGroup cavasGroup)
        {
            _canvasGroup = cavasGroup;
        }
#endif
    }
}
