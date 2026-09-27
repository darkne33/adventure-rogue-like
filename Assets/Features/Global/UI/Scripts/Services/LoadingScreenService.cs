using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UI
{
    public sealed class LoadingScreenService : ILoadingScreenService
    {
        private const float MinimumDisplayDuration = 0.35f;

        private readonly LoadingPanel _loadingPanelPrefab;

        private LoadingPanelPresenter _presenter;
        private float _shownAt;
        private bool _isPlaying;

        public bool IsLoading => _presenter?.Panel != null;

        public LoadingScreenService(LoadingPanel loadingPanelPrefab)
        {
            _loadingPanelPrefab = loadingPanelPrefab;
        }

        public UniTask Show(CancellationToken cancellationToken = default) =>
            ShowInternal(null, cancellationToken);

        private async UniTask ShowInternal(RenderTexture capturedFrame,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (IsLoading)
                return;

            if (_loadingPanelPrefab == null)
                throw new InvalidOperationException("The loading panel prefab is not configured.");

            // This screen must exist before Addressables, the camera and the regular UI root.
            LoadingPanel panel = UnityEngine.Object.Instantiate(_loadingPanelPrefab);
            UnityEngine.Object.DontDestroyOnLoad(panel.gameObject);
            _presenter = new LoadingPanelPresenter();
            _presenter.Construct(panel);
            if (capturedFrame != null)
                panel.PreparePixelatedCover(capturedFrame);
            await _presenter.Initialize();
            _presenter.ForceShow();
            _shownAt = Time.unscaledTime;

            await UniTask.NextFrame(cancellationToken: cancellationToken);
        }

        public async UniTask Hide()
        {
            LoadingPanelPresenter presenter = _presenter;
            _presenter = null;
            if (presenter?.Panel == null)
                return;

            LoadingPanel panel = presenter.Panel;
            try
            {
                presenter.ForceHide();
                panel.gameObject.SetActive(false);
                await presenter.OnClosed();
            }
            finally
            {
                UnityEngine.Object.Destroy(panel.gameObject);
            }
        }

        public UniTask Play(Func<UniTask> loadingAction, Action beforeHide = null,
            CancellationToken cancellationToken = default) =>
            PlayInternal(loadingAction, beforeHide, false, true, cancellationToken);

        public UniTask PlayPixelated(Func<UniTask> loadingAction, Action beforeHide = null,
            CancellationToken cancellationToken = default, bool showLoading = true) =>
            PlayInternal(loadingAction, beforeHide, true, showLoading, cancellationToken);

        private async UniTask PlayInternal(Func<UniTask> loadingAction, Action beforeHide,
            bool pixelated, bool showLoading, CancellationToken cancellationToken)
        {
            if (loadingAction == null)
                throw new ArgumentNullException(nameof(loadingAction));

            if (_isPlaying)
                throw new InvalidOperationException("A loading screen is already active.");

            if (pixelated && IsLoading)
                throw new InvalidOperationException("Pixelation requires the current screen to be visible.");

            cancellationToken.ThrowIfCancellationRequested();
            _isPlaying = true;
            RenderTexture capturedFrame = null;

            try
            {
                if (pixelated)
                {
                    // Capture after both the camera and overlay UI have rendered,
                    // before the loading panel covers the current screen.
                    await UniTask.WaitForEndOfFrame(cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    capturedFrame = new RenderTexture(Mathf.Max(1, Screen.width),
                        Mathf.Max(1, Screen.height), 0, RenderTextureFormat.ARGB32)
                    {
                        name = "PixelatedScreenTransition",
                        filterMode = FilterMode.Point,
                        wrapMode = TextureWrapMode.Clamp
                    };
                    if (!capturedFrame.Create())
                        throw new InvalidOperationException("Could not capture the screen for the pixelated transition.");
                    ScreenCapture.CaptureScreenshotIntoRenderTexture(capturedFrame);
                }

                // Reuse the screen shown by BootstrapState without hiding or recreating it.
                await ShowInternal(capturedFrame, cancellationToken);
                if (pixelated)
                {
                    await _presenter.Panel.PlayPixelatedCoverAsync(cancellationToken, showLoading);
                    _shownAt = Time.unscaledTime;
                    // Let the fully covered frame render before level preparation blocks it.
                    await UniTask.NextFrame(cancellationToken: cancellationToken);
                }

                await loadingAction();
                await UniTask.NextFrame(cancellationToken: cancellationToken);

                float minimumDuration = showLoading ? MinimumDisplayDuration : 0f;
                float remainingDuration = minimumDuration - (Time.unscaledTime - _shownAt);
                if (remainingDuration > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(remainingDuration),
                        ignoreTimeScale: true, cancellationToken: cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();
                beforeHide?.Invoke();
                await _presenter.Hide();
            }
            finally
            {
                try
                {
                    await Hide();
                }
                finally
                {
                    if (capturedFrame != null)
                    {
                        capturedFrame.Release();
                        UnityEngine.Object.Destroy(capturedFrame);
                    }
                    _isPlaying = false;
                }
            }
        }
    }

    public interface ILoadingScreenService
    {
        bool IsLoading { get; }
        UniTask Show(CancellationToken cancellationToken = default);
        UniTask Hide();
        UniTask Play(Func<UniTask> loadingAction, Action beforeHide = null,
            CancellationToken cancellationToken = default);
        UniTask PlayPixelated(Func<UniTask> loadingAction, Action beforeHide = null,
            CancellationToken cancellationToken = default, bool showLoading = true);
    }
}
