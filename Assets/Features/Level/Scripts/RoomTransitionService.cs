using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UI;
using UnityEngine;

namespace Features.Enemies.Scripts.Level.Scripts
{
    public class RoomTransitionService : IRoomTransitionService
    {
        private const string RadiusProperty = "_Radius";
        private const string AspectProperty = "_Aspect";
        private const string SoftnessProperty = "_Softness";
        private const float CoverDuration = 0.55f;
        private const float RevealDuration = 0.5f;
        private const float MinimumLoadingDisplayDuration = 0.35f;
        private const float DefaultBeforeRevealDelay = 0.08f;

        private readonly IPanelService _panelService;

        private RoomTransitionPanel _panel;
        private Material _irisMaterial;
        private float _openRadius;
        private float _closedRadius;
        private Sequence _sequence;

        public bool IsPlaying { get; private set; }

        public RoomTransitionService(IPanelService panelService)
        {
            _panelService = panelService;
        }

        public UniTask Play(Func<UniTask> hiddenAction, Action beforeReveal = null) =>
            PlayInternal(hiddenAction, beforeReveal, showLoading: false, DefaultBeforeRevealDelay);

        public UniTask Play(Func<UniTask> hiddenAction, float beforeRevealDelay,
            Action beforeReveal = null) =>
            PlayInternal(hiddenAction, beforeReveal, showLoading: false, beforeRevealDelay);

        public UniTask PlayLoading(Func<UniTask> hiddenAction, Action beforeReveal = null) =>
            PlayInternal(hiddenAction, beforeReveal, showLoading: true, DefaultBeforeRevealDelay);

        private async UniTask PlayInternal(Func<UniTask> hiddenAction, Action beforeReveal,
            bool showLoading, float beforeRevealDelay)
        {
            if (hiddenAction == null)
                throw new ArgumentNullException(nameof(hiddenAction));

            if (IsPlaying)
                return;

            IsPlaying = true;
            await EnsurePanel();
            _panel.gameObject.SetActive(true);
            _panel.transform.SetAsLastSibling();
            _panel.TransitionCanvasGroup.blocksRaycasts = true;
            _panel.SetLoadingVisible(false);

            try
            {
                await AnimateCover();

                float loadingShownAt = 0f;
                if (showLoading)
                {
                    _panel.SetLoadingVisible(true);
                    loadingShownAt = Time.realtimeSinceStartup;
                }

                await hiddenAction();

                // Panels created during the hidden action (for example the gameplay HUD)
                // must stay below the transition overlay.
                _panel.transform.SetAsLastSibling();

                if (showLoading)
                {
                    float elapsed = Time.realtimeSinceStartup - loadingShownAt;
                    int remainingMilliseconds = Mathf.CeilToInt(
                        Mathf.Max(0f, MinimumLoadingDisplayDuration - elapsed) * 1000f);
                    if (remainingMilliseconds > 0)
                    {
                        await UniTask.Delay(remainingMilliseconds, ignoreTimeScale: true);
                    }
                }

                int beforeRevealDelayMilliseconds = Mathf.CeilToInt(
                    Mathf.Max(0f, beforeRevealDelay) * 1000f);
                if (beforeRevealDelayMilliseconds > 0)
                    await UniTask.Delay(beforeRevealDelayMilliseconds, ignoreTimeScale: true);

                beforeReveal?.Invoke();
                _panel.SetLoadingVisible(false);
                await AnimateReveal();
            }
            finally
            {
                _sequence?.Kill();
                if (_panel != null)
                {
                    _panel.SetLoadingVisible(false);
                    _panel.TransitionCanvasGroup.blocksRaycasts = false;
                    _panel.gameObject.SetActive(false);
                }

                IsPlaying = false;
            }
        }

        private async UniTask AnimateCover()
        {
            SetRadius(_openRadius);

            _sequence = DOTween.Sequence().SetUpdate(true);
            _ = _sequence.Append(DOTween.To(GetRadius, SetRadius, _closedRadius, CoverDuration)
                .SetEase(Ease.InOutCubic));

            await _sequence.ToUniTask();
        }

        private async UniTask AnimateReveal()
        {
            _sequence = DOTween.Sequence().SetUpdate(true);
            _ = _sequence.Append(DOTween.To(GetRadius, SetRadius, _openRadius, RevealDuration)
                .SetEase(Ease.OutCubic));
            await _sequence.ToUniTask();
        }

        private float GetRadius() =>
            _irisMaterial.GetFloat(RadiusProperty);

        private void SetRadius(float radius) =>
            _irisMaterial.SetFloat(RadiusProperty, radius);

        private async UniTask EnsurePanel()
        {
            if (_panel != null)
                return;

            RoomTransitionPanelPresenter presenter =
                await _panelService.OpenPanelWithPresenterHidden<RoomTransitionPanelPresenter>(
                    PanelName.RoomTransitionPanel);

            _panel = presenter.Panel;
            _panel.TransitionCanvasGroup.alpha = 1f;
            _panel.TransitionCanvasGroup.blocksRaycasts = false;
            _panel.SetLoadingVisible(false);

            _irisMaterial = _panel.IrisImage.material;
            if (_irisMaterial == null)
                throw new InvalidOperationException("The room transition iris material is not configured.");

            Rect rect = _panel.IrisImage.rectTransform.rect;
            float aspect = rect.height > 0f ? rect.width / rect.height : (float)Screen.width / Screen.height;
            float softness = _irisMaterial.GetFloat(SoftnessProperty);

            _irisMaterial.SetFloat(AspectProperty, aspect);
            _openRadius = Mathf.Sqrt(aspect * aspect * 0.25f + 0.25f) + softness;
            _closedRadius = -softness;
            SetRadius(_openRadius);

            _panel.gameObject.SetActive(false);
        }
    }

    public interface IRoomTransitionService
    {
        bool IsPlaying { get; }
        UniTask Play(Func<UniTask> hiddenAction, Action beforeReveal = null);
        UniTask Play(Func<UniTask> hiddenAction, float beforeRevealDelay, Action beforeReveal = null);
        UniTask PlayLoading(Func<UniTask> hiddenAction, Action beforeReveal = null);
    }
}
