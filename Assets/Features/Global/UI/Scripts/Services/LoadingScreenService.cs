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

        public async UniTask Show(CancellationToken cancellationToken = default)
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

        public async UniTask Play(Func<UniTask> loadingAction, Action beforeHide = null,
            CancellationToken cancellationToken = default)
        {
            if (loadingAction == null)
                throw new ArgumentNullException(nameof(loadingAction));

            if (_isPlaying)
                throw new InvalidOperationException("A loading screen is already active.");

            cancellationToken.ThrowIfCancellationRequested();
            _isPlaying = true;

            try
            {
                // Reuse the screen shown by BootstrapState without hiding or recreating it.
                await Show(cancellationToken);
                await loadingAction();
                await UniTask.NextFrame(cancellationToken: cancellationToken);

                float remainingDuration = MinimumDisplayDuration - (Time.unscaledTime - _shownAt);
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
    }
}
