using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.Leaderboard;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class MainMenuPanelPresenter : PanelPresenter<MainMenuPanel>
{
    private const int ShowAnimationMilliseconds = 300;

    private readonly ILeaderboardService _leaderboardService;

    private bool _playRequested;
    private LeaderboardView _leaderboardView;

    public MainMenuPanelPresenter(ILeaderboardService leaderboardService) =>
        _leaderboardService = leaderboardService;

    public override UniTask Initialize()
    {
        _playRequested = false;
        _leaderboardView = Panel.EnsureLeaderboardView();
        Panel.SetButtonsInteractable(false);
        Panel.PlayButton.onClick.AddListener(RequestPlay);

        if (_leaderboardService.IsConfigured)
            RefreshLeaderboard(Panel.GetCancellationTokenOnDestroy()).Forget();
        else
            _leaderboardView.ShowError("SET PLAYFAB TITLE ID");

        EnableInputAfterShow(Panel.GetCancellationTokenOnDestroy()).Forget();

        return UniTask.CompletedTask;
    }

    public UniTask WaitForPlay(CancellationToken cancellationToken) =>
        UniTask.WaitUntil(() => _playRequested, cancellationToken: cancellationToken);

    public override UniTask OnClosed()
    {
        if (Panel != null)
            Panel.PlayButton.onClick.RemoveListener(RequestPlay);

        _leaderboardView = null;

        return base.OnClosed();
    }

    private void RequestPlay()
    {
        if (_playRequested)
            return;

        _playRequested = true;
        Panel.SetButtonsInteractable(false);
    }

    private async UniTask RefreshLeaderboard(CancellationToken cancellationToken)
    {
        LeaderboardView view = _leaderboardView;
        if (view == null)
            return;

        view.ShowLoading();

        try
        {
            var entries = await _leaderboardService.GetTop(cancellationToken);
            if (view != null)
                view.ShowEntries(entries, _leaderboardService.PlayerId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (PlayFabLeaderboardException exception)
        {
            if (view != null)
                view.ShowError(exception.GetUserMessage());
            Debug.LogWarning(exception.Message);
        }
        catch (Exception exception)
        {
            if (view != null)
                view.ShowError("LEADERBOARD IS UNAVAILABLE");
            Debug.LogException(exception);
        }
    }

    private async UniTask EnableInputAfterShow(CancellationToken cancellationToken)
    {
        await UniTask.Delay(ShowAnimationMilliseconds, ignoreTimeScale: true,
            cancellationToken: cancellationToken);

        if (Panel == null)
            return;

        Panel.SetButtonsInteractable(true);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(Panel.PlayButton.gameObject);
    }
}
