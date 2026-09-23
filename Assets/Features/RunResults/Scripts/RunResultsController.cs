using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Features.Quests.Scripts;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace Features.RunResults.Scripts
{
    public sealed class RunResultsController : IDisposable
    {
        private const float ShowDuration = 0.24f;

        private readonly IPanelsProvider _panelsProvider;
        private readonly IPauseService _pauseService;
        private readonly ITimeScaleService _timeScale;
        private readonly ICursorService _cursorService;
        private readonly RunRestartService _runRestart;
        private readonly RunResultsTracker _tracker;
        private readonly QuestRunTracker _questRunTracker;
        private readonly RunResultsPanel _prefab;
        private readonly DiContainer _container;

        private RunResultsPanel _panel;
        private string _sceneName;
        private bool _isOpen;
        private bool _isLeaving;
        private bool _ownsPause;
        private bool _disposed;

        public RunResultsController(IPanelsProvider panelsProvider, IPauseService pauseService,
            ITimeScaleService timeScale, ICursorService cursorService, RunRestartService runRestart,
            RunResultsTracker tracker, QuestRunTracker questRunTracker,
            RunResultsPanel prefab, DiContainer container)
        {
            _panelsProvider = panelsProvider;
            _pauseService = pauseService;
            _timeScale = timeScale;
            _cursorService = cursorService;
            _runRestart = runRestart;
            _tracker = tracker;
            _questRunTracker = questRunTracker;
            _prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
            _container = container;
        }

        public void Show(string sceneName)
        {
            if (_disposed || _isOpen || _runRestart.IsRestarting)
                return;

            if (_panel == null)
                CreateView();

            _sceneName = sceneName;
            _panel.SetResults(_tracker.CaptureResults());
            _questRunTracker.EndRun();
            _isOpen = true;
            _ownsPause = !_timeScale.IsPaused;
            _pauseService.HandlePause();
            _cursorService.ShowUiCursor();

            _panel.SetStatus(string.Empty);
            _panel.SetButtonsInteractable(false);
            _panel.gameObject.SetActive(true);
            _panel.transform.SetAsLastSibling();
            _panel.CanvasGroup.alpha = 0f;
            _panel.CanvasGroup.interactable = false;
            _panel.CanvasGroup.blocksRaycasts = true;
            _panel.ContentRoot.localScale = Vector3.one * 0.96f;

            _panel.ContentRoot.DOScale(1f, ShowDuration).SetEase(Ease.OutCubic).SetUpdate(true);
            _panel.CanvasGroup.DOFade(1f, ShowDuration).SetUpdate(true).OnComplete(() =>
            {
                if (_disposed || _panel == null || _isLeaving)
                    return;

                _panel.CanvasGroup.interactable = true;
                _panel.SetButtonsInteractable(true);
                SelectRetry();
            });
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            if (_ownsPause)
            {
                _pauseService.CancelPause();
                _ownsPause = false;
            }

            if (_isOpen)
                _cursorService.ShowGameplayCursor();

            if (_panel == null)
                return;

            _panel.RetryRequested -= Retry;
            _panel.MenuRequested -= ReturnToMenu;
            _panel.ContentRoot.DOKill();
            _panel.CanvasGroup.DOKill();
            UnityEngine.Object.Destroy(_panel.gameObject);
            _panel = null;
        }

        private void CreateView()
        {
            Transform root = _panelsProvider.GetRootFor(PanelLocation.PopUp);
            _panel = _container.InstantiatePrefabForComponent<RunResultsPanel>(_prefab, root);
            _panel.RetryRequested += Retry;
            _panel.MenuRequested += ReturnToMenu;
            _panel.gameObject.SetActive(false);
        }

        private void Retry() => LeaveResults(true).Forget();

        private void ReturnToMenu() => LeaveResults(false).Forget();

        private async UniTask LeaveResults(bool retry)
        {
            if (_disposed || !_isOpen || _isLeaving || _runRestart.IsRestarting)
                return;

            _isLeaving = true;
            _panel.SetButtonsInteractable(false);
            _panel.SetStatus(retry ? "RESTARTING RUN..." : "RETURNING TO MAIN MENU...");
            // The scene transition releases the pause before unloading this controller.
            _ownsPause = false;

            try
            {
                bool completed = retry
                    ? await _runRestart.Restart(_sceneName)
                    : await _runRestart.ReturnToMainMenu(_sceneName);

                if (_disposed || _panel == null)
                    return;

                if (completed)
                {
                    _isOpen = false;
                    _panel.gameObject.SetActive(false);
                }
                else
                {
                    RestoreAfterFailure(retry);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (!_disposed && _panel != null)
                    RestoreAfterFailure(retry);
            }
            finally
            {
                _isLeaving = false;
            }
        }

        private void RestoreAfterFailure(bool retry)
        {
            _pauseService.HandlePause();
            _ownsPause = true;
            _cursorService.ShowUiCursor();
            _panel.SetStatus(retry ? "RESTART FAILED. PLEASE TRY AGAIN." : "RETURN FAILED. PLEASE TRY AGAIN.");
            _panel.SetButtonsInteractable(true);
            SelectRetry();
        }

        private void SelectRetry()
        {
            if (_panel.RetryButton != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(_panel.RetryButton.gameObject);
        }
    }
}
