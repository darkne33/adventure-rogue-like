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
    private bool _isCharacterSelectionOpen;
    private LeaderboardView _leaderboardView;
    private CharacterSelectionView _characterSelectionView;
    private CharacterConfiguration _characterConfiguration;

    public MainMenuPanelPresenter(ILeaderboardService leaderboardService) =>
        _leaderboardService = leaderboardService;

    public override UniTask Initialize()
    {
        _playRequested = false;
        _isCharacterSelectionOpen = false;
        _leaderboardView = Panel.EnsureLeaderboardView();
        _characterSelectionView = Panel.CharacterSelection;
        _characterConfiguration = Panel.CharacterConfiguration;

        if (_characterSelectionView == null)
            throw new InvalidOperationException("CharacterSelectionPanel prefab is not assigned to the main menu panel.");

        if (_characterConfiguration == null)
            throw new InvalidOperationException("PlayerConfiguration is not assigned to the main menu panel.");

        if (!_characterConfiguration.HasCharacters)
            throw new InvalidOperationException("PlayerConfiguration does not contain any characters.");

        _characterConfiguration.ValidateRosterEntries();

        Panel.SetHomeVisible(true);
        _characterSelectionView.Hide();
        Panel.SetButtonsInteractable(false);
        Panel.PlayButton.onClick.AddListener(OpenCharacterSelection);
        _characterSelectionView.SelectionRequested += SelectCharacter;
        _characterSelectionView.StartRequested += RequestPlay;
        _characterSelectionView.BackRequested += ReturnToMainMenu;

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
        {
            Panel.PlayButton.onClick.RemoveListener(OpenCharacterSelection);

            if (_characterSelectionView != null)
            {
                _characterSelectionView.SelectionRequested -= SelectCharacter;
                _characterSelectionView.StartRequested -= RequestPlay;
                _characterSelectionView.BackRequested -= ReturnToMainMenu;
            }
        }

        _leaderboardView = null;
        _characterSelectionView = null;
        _characterConfiguration = null;

        return base.OnClosed();
    }

    private void RequestPlay()
    {
        if (_playRequested || !_characterConfiguration.SelectedCharacter.IsConfigured)
            return;

        _playRequested = true;
        Panel.SetButtonsInteractable(false);
        _characterSelectionView.SetInteractable(false);
    }

    private void OpenCharacterSelection()
    {
        if (_playRequested || _isCharacterSelectionOpen)
            return;

        _isCharacterSelectionOpen = true;
        Panel.SetButtonsInteractable(false);
        Panel.SetHomeVisible(false);
        _characterConfiguration.ResetSelectionToDefault();
        _characterSelectionView.Show(_characterConfiguration.Characters,
            _characterConfiguration.SelectedCharacterIndex);
    }

    private void SelectCharacter(int index)
    {
        _characterConfiguration.SelectCharacter(index);
        _characterSelectionView.SetSelectedIndex(index);
    }

    private void ReturnToMainMenu()
    {
        if (_playRequested || !_isCharacterSelectionOpen)
            return;

        _isCharacterSelectionOpen = false;
        _characterSelectionView.Hide();
        Panel.SetHomeVisible(true);
        Panel.SetButtonsInteractable(true);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(Panel.PlayButton.gameObject);
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
