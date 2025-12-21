using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace UI
{
    [Serializable]
    public class CanvasFadeAnimations : IPanelAnimation
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _showTime = .25f;
        [SerializeField] private float _hideTime = .25f;
        [SerializeField] private float _delayBeforeShow;

        private CancellationTokenSource _cts;

        public void Initialize()
        {
        }

        public UniTask Show() =>
            _canvasGroup
                .DOFade(1, _showTime)
                .From(0)
                .SetDelay(_delayBeforeShow)
                .SetId($"{_canvasGroup.gameObject.name} ShowFade")
                .ToUniTask(cancellationToken: GetToken());

        public UniTask Hide() =>
            _canvasGroup
                .DOFade(0, _hideTime)
                .SetId($"{_canvasGroup.gameObject.name} Hide")
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