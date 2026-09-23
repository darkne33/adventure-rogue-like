using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.Leaderboard;
using Features.Quests.Scripts;
using Features.Sounds;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class MainMenuPanelPresenter : PanelPresenter<MainMenuPanel>
{
    private readonly ILeaderboardService _leaderboardService;
    private readonly RunRestartService _runRestartService;
    private readonly ISoundsService _soundsService;
    private readonly QuestService _questService;

    private bool _playRequested;
    private bool _isCharacterSelectionOpen;
    private bool _isQuestsOpen;
    private bool _isUnlocksOpen;
    private LeaderboardView _leaderboardView;
    private CharacterSelectionView _characterSelectionView;
    private CharacterConfiguration _characterConfiguration;
    private QuestsPanelView _questsView;
    private UnlocksPanelView _unlocksView;

    public MainMenuPanelPresenter(ILeaderboardService leaderboardService,
        RunRestartService runRestartService, ISoundsService soundsService, QuestService questService)
    {
        _leaderboardService = leaderboardService;
        _runRestartService = runRestartService;
        _soundsService = soundsService;
        _questService = questService;
    }

    public override async UniTask Initialize()
    {
        _playRequested = false;
        _isCharacterSelectionOpen = false;
        _isQuestsOpen = false;
        _isUnlocksOpen = false;
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
        _characterSelectionView.SetOwnershipResolver(_questService.IsCharacterOwned);

        Panel.SetHomeVisible(true);
        Panel.SetSilverBalanceVisible(true);
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
        Panel.QuestsButton.onClick.AddListener(OpenQuests);
        Panel.UnlocksButton.onClick.AddListener(OpenUnlocks);
        _questsView = QuestsPanelView.Create(Panel.QuestsPanelPrefab, Panel.transform, _questService,
            _characterSelectionView.GetPortrait, _characterSelectionView.PortraitMaterial);
        _questsView.BackRequested += CloseQuests;
        _questsView.Hide();
        _unlocksView = UnlocksPanelView.Create(Panel.UnlocksPanelPrefab, Panel.transform, _questService,
            _characterSelectionView.GetPortrait, _characterSelectionView.PortraitMaterial);
        _unlocksView.BackRequested += CloseUnlocks;
        _unlocksView.Hide();
        _characterSelectionView.SelectionRequested += SelectCharacter;
        _characterSelectionView.StartRequested += RequestPlay;
        _characterSelectionView.BackRequested += ReturnToMainMenu;

        _questService.Changed += RefreshProgression;
        RefreshProgression();

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
        _questService.Changed -= RefreshProgression;

        if (Panel != null)
        {
            Panel.PlayButton.onClick.RemoveListener(OpenCharacterSelection);
            Panel.QuestsButton.onClick.RemoveListener(OpenQuests);
            Panel.UnlocksButton.onClick.RemoveListener(OpenUnlocks);

            if (_questsView != null)
            {
                _questsView.BackRequested -= CloseQuests;
                _questsView.Hide();
                UnityEngine.Object.Destroy(_questsView.gameObject);
            }

            if (_unlocksView != null)
            {
                _unlocksView.BackRequested -= CloseUnlocks;
                _unlocksView.Hide();
                UnityEngine.Object.Destroy(_unlocksView.gameObject);
            }

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
        _questsView = null;
        _unlocksView = null;
        _isQuestsOpen = false;
        _isUnlocksOpen = false;

        return base.OnClosed();
    }

    private void RequestPlay()
    {
        if (_playRequested || !_characterConfiguration.SelectedCharacter.IsConfigured ||
            !_questService.IsCharacterOwned(_characterConfiguration.SelectedCharacter.Id))
            return;

        _soundsService.Play(SoundId.UiStartClick);
        _playRequested = true;
        Panel.Room.CancelCameraTransition();
        Panel.SetButtonsInteractable(false);
        _characterSelectionView.SetInteractable(false);
    }

    private void OpenCharacterSelection()
    {
        if (_playRequested || _isCharacterSelectionOpen || _isQuestsOpen || _isUnlocksOpen)
            return;

        _isCharacterSelectionOpen = true;
        Panel.SetButtonsInteractable(false);
        Panel.SetHomeVisible(false);
        Panel.SetSilverBalanceVisible(false);
        _characterSelectionView.Show(_characterConfiguration.Characters,
            _characterConfiguration.SelectedCharacterIndex, previewAlreadyShown: true);
        _characterSelectionView.RefreshOwnership();
        MoveToCharacterSelectionAsync().Forget();
    }

    private void OpenQuests()
    {
        if (_playRequested || _isCharacterSelectionOpen || _isQuestsOpen || _isUnlocksOpen || _questsView == null)
            return;

        _isQuestsOpen = true;
        Panel.SetButtonsInteractable(false);
        Panel.SetHomeVisible(false);
        _questsView.Show();
        Panel.SetSilverBalanceVisible(true);
    }

    private void CloseQuests()
    {
        if (_playRequested || !_isQuestsOpen)
            return;

        _isQuestsOpen = false;
        _questsView.Hide();
        Panel.SetHomeVisible(true);
        EnableInput();
        _soundsService.Play(SoundId.UiSelect);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(Panel.QuestsButton.gameObject);
    }

    private void OpenUnlocks()
    {
        if (_playRequested || _isCharacterSelectionOpen || _isQuestsOpen || _isUnlocksOpen || _unlocksView == null)
            return;

        _isUnlocksOpen = true;
        Panel.SetButtonsInteractable(false);
        Panel.SetHomeVisible(false);
        _unlocksView.Show();
        Panel.SetSilverBalanceVisible(true);
    }

    private void CloseUnlocks()
    {
        if (_playRequested || !_isUnlocksOpen)
            return;

        _isUnlocksOpen = false;
        _unlocksView.Hide();
        _characterSelectionView.RefreshOwnership();
        Panel.SetHomeVisible(true);
        EnableInput();
        _soundsService.Play(SoundId.UiSelect);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(Panel.UnlocksButton.gameObject);
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
        Panel.SetSilverBalanceVisible(true);
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

    private void RefreshProgression()
    {
        if (Panel != null)
        {
            Panel.SetSilverBalance(_questService.Silver);
            Panel.SetProgressionAlerts(_questService.HasClaimableRewards, _questService.HasNewUnlocks);
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

        if (_playRequested || _isCharacterSelectionOpen || _isQuestsOpen || _isUnlocksOpen)
            return;

        Panel.SetButtonsInteractable(true);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(Panel.PlayButton.gameObject);
    }
}
