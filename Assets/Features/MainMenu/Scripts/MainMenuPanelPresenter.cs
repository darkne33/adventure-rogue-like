using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.Leaderboard;
using Features.Sounds;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class MainMenuPanelPresenter : PanelPresenter<MainMenuPanel>
{
    private readonly ILeaderboardService _leaderboardService;
    private readonly RunRestartService _runRestartService;
    private readonly ISoundsService _soundsService;

    private bool _playRequested;
    private bool _isCharacterSelectionOpen;
    private LeaderboardView _leaderboardView;
    private CharacterSelectionView _characterSelectionView;
    private CharacterConfiguration _characterConfiguration;

    public MainMenuPanelPresenter(ILeaderboardService leaderboardService,
        RunRestartService runRestartService, ISoundsService soundsService)
    {
        _leaderboardService = leaderboardService;
        _runRestartService = runRestartService;
        _soundsService = soundsService;
    }

    public override async UniTask Initialize()
    {
        _playRequested = false;
        _isCharacterSelectionOpen = false;
        _leaderboardView = Panel.Leaderboard;
        _characterSelectionView = Panel.CharacterSelection;
        _characterConfiguration = Panel.CharacterConfiguration;

        if (_characterSelectionView == null)
            throw new InvalidOperationException("CharacterSelectionPanel prefab is not assigned to the main menu panel.");

        if (_characterConfiguration == null)
            throw new InvalidOperationException("PlayerConfiguration is not assigned to the main menu panel.");

        if (!_characterConfiguration.HasCharacters)
            throw new InvalidOperationException("PlayerConfiguration does not contain any characters.");

        _characterConfiguration.ValidateRosterEntries();
        _characterConfiguration.ResetSelectionToDefault();

        Panel.SetHomeVisible(true);
        _characterSelectionView.Hide();
        Panel.SetButtonsInteractable(false);
        Panel.CreateRoom();
        _characterSelectionView.SetWorldStage(Panel.Room.CharacterSpawnPoint);

        // Prepare every roster portrait while the menu is still hidden, before selection can open.
        CancellationToken cancellationToken = Panel.GetCancellationTokenOnDestroy();
        await _characterSelectionView.PrewarmPortraitsAsync(
            _characterConfiguration.Characters, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        await _characterSelectionView.PreparePreviewAsync(
            _characterConfiguration.SelectedCharacter, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        Panel.PlayButton.onClick.AddListener(OpenCharacterSelection);
        _characterSelectionView.SelectionRequested += SelectCharacter;
        _characterSelectionView.StartRequested += RequestPlay;
        _characterSelectionView.BackRequested += ReturnToMainMenu;

        if (_runRestartService.ConsumeCharacterSelectionEntryRequest())
            OpenCharacterSelection();

        if (_leaderboardService.IsConfigured)
            RefreshLeaderboard(cancellationToken).Forget();
        else
            _leaderboardView.ShowError("SET PLAYFAB TITLE ID");

        EnableInput();
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
                _characterSelectionView.Hide();
            }

            Panel.CloseRoom();
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

        _soundsService.Play(SoundId.UiStartClick);
        _playRequested = true;
        Panel.Room.CancelCameraTransition();
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
        _characterSelectionView.Show(_characterConfiguration.Characters,
            _characterConfiguration.SelectedCharacterIndex, previewAlreadyShown: true);
        MoveToCharacterSelectionAsync().Forget();
    }

    private async UniTask MoveToCharacterSelectionAsync()
    {
        try
        {
            CancellationToken cancellationToken = Panel.GetCancellationTokenOnDestroy();
            await Panel.Room.MoveToCharacterAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void SelectCharacter(int index, int direction)
    {
        _characterConfiguration.SelectCharacter(index);
        _characterSelectionView.SetSelectedIndex(index, direction);
        _soundsService.Play(SoundId.UiSelect);
    }

    private void ReturnToMainMenu()
    {
        if (_playRequested || !_isCharacterSelectionOpen)
            return;

        _isCharacterSelectionOpen = false;
        _characterSelectionView.Hide(keepPreview: true);
        Panel.SetHomeVisible(true);
        EnableInput();
        MoveToMainMenuAsync().Forget();
    }

    private async UniTask MoveToMainMenuAsync()
    {
        try
        {
            await Panel.Room.MoveToMenuAsync(Panel.GetCancellationTokenOnDestroy());
        }
        catch (OperationCanceledException)
        {
        }
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

    private void EnableInput()
    {
        if (Panel == null)
            return;

        if (_isCharacterSelectionOpen)
            return;

        Panel.SetButtonsInteractable(true);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(Panel.PlayButton.gameObject);
    }
}
