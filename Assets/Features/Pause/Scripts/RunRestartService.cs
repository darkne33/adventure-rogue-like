using System;
using Core;
using Cysharp.Threading.Tasks;
using UI;
using UnityEngine;
using Zenject;

public sealed class RunRestartService
{
    private readonly ISceneLoader _sceneLoader;
    private readonly IGameModeService _gameModeService;
    private readonly ICharacterLevelService _characterLevelService;
    private readonly IPauseService _pauseService;
    private readonly IPanelService _panelService;
    private readonly GameStateMachine _gameStateMachine;

    public bool IsRestarting { get; private set; }
    public Exception LastError { get; private set; }

    public RunRestartService(ISceneLoader sceneLoader, IGameModeService gameModeService,
        ICharacterLevelService characterLevelService, IPauseService pauseService,
        IPanelService panelService, GameStateMachine gameStateMachine)
    {
        _sceneLoader = sceneLoader;
        _gameModeService = gameModeService;
        _characterLevelService = characterLevelService;
        _pauseService = pauseService;
        _panelService = panelService;
        _gameStateMachine = gameStateMachine;
    }

    public async UniTask<bool> Restart(string sceneName)
    {
        if (IsRestarting)
            return false;

        IsRestarting = true;
        LastError = null;

        try
        {
            if (string.IsNullOrWhiteSpace(sceneName) ||
                !_sceneLoader.HasActiveScene(sceneName))
                throw new InvalidOperationException(
                    $"Addressable game scene '{sceneName}' is not active.");

            await _panelService.HidePanelForce(PanelName.RoomTransitionPanel);
            await _sceneLoader.UnloadScene(sceneName);

            _pauseService.CancelPause();
            _gameModeService.Remove<RogueLikeStateMachine>();
            _characterLevelService.Reset();
            await _sceneLoader.LoadSceneFromAddressable(sceneName);

            RogueLikeSceneProvider sceneProvider =
                _sceneLoader.GetGameSceneComponentsProvider<RogueLikeSceneProvider>(sceneName);
            if (sceneProvider == null)
                throw new InvalidOperationException(
                    $"Scene {sceneName} does not contain {nameof(RogueLikeSceneProvider)}.");

            SceneContext sceneContext = sceneProvider.GetSceneContext();
            _gameModeService.Add<RogueLikeStateMachine>(sceneContext.Container);

            sceneProvider.EnableScene();
            await _gameModeService.Get<RogueLikeStateMachine>()
                .EnterState<RogueLikePrepareStatsState>();
            return true;
        }
        catch (Exception exception)
        {
            LastError = exception;
            Debug.LogException(exception);
            return false;
        }
        finally
        {
            IsRestarting = false;
        }
    }

    public async UniTask<bool> ReturnToMainMenu(string sceneName)
    {
        if (IsRestarting)
            return false;

        IsRestarting = true;
        LastError = null;

        try
        {
            if (string.IsNullOrWhiteSpace(sceneName) ||
                !_sceneLoader.HasActiveScene(sceneName))
                throw new InvalidOperationException(
                    $"Addressable game scene '{sceneName}' is not active.");

            await _panelService.HidePanelForce(PanelName.RoomTransitionPanel);
            await _panelService.HidePanelForce(PanelName.CharacterPanel);
            await _sceneLoader.UnloadScene(sceneName);

            _pauseService.CancelPause();
            _gameModeService.Remove<RogueLikeStateMachine>();
            _characterLevelService.Reset();

            await _gameStateMachine.EnterState<LoadRogueLikeGameSceneState>();
            return true;
        }
        catch (Exception exception)
        {
            LastError = exception;
            Debug.LogException(exception);
            return false;
        }
        finally
        {
            IsRestarting = false;
        }
    }
}
