using System;
using Core;
using Cysharp.Threading.Tasks;
using Features.Enemies.Scripts;
using Features.Enemies.Scripts.Level.Scripts;
using UnityEngine;

public sealed class LevelProgressionService : ILevelProgressionService, IDisposable
{
    private readonly LevelsConfiguration _levelsConfiguration;
    private readonly ILevelFactory _levelFactory;
    private readonly IRogueLikeRuntimeDataService _runtimeDataService;
    private readonly ISceneService<RogueLikeSceneProvider> _sceneService;
    private readonly ICharacterProvider _characterProvider;
    private readonly IEnemiesProvider _enemiesProvider;
    private readonly IRoomTransitionService _roomTransitionService;
    private readonly EnemiesWaveObserver _enemiesWaveObserver;
    private readonly IGameModeService _gameModeService;

    private bool _isTransitioning;
    private bool _isRunCompleted;

    public LevelProgressionService(LevelsConfiguration levelsConfiguration, ILevelFactory levelFactory,
        IRogueLikeRuntimeDataService runtimeDataService, ISceneService<RogueLikeSceneProvider> sceneService,
        ICharacterProvider characterProvider, IEnemiesProvider enemiesProvider,
        IRoomTransitionService roomTransitionService, EnemiesWaveObserver enemiesWaveObserver,
        IGameModeService gameModeService)
    {
        _levelsConfiguration = levelsConfiguration;
        _levelFactory = levelFactory;
        _runtimeDataService = runtimeDataService;
        _sceneService = sceneService;
        _characterProvider = characterProvider;
        _enemiesProvider = enemiesProvider;
        _roomTransitionService = roomTransitionService;
        _enemiesWaveObserver = enemiesWaveObserver;
        _gameModeService = gameModeService;

        _enemiesWaveObserver.RoomCompleted += HandleRoomCompleted;
    }

    public void TransitToNextLevel()
    {
        int nextLevelIndex = _runtimeDataService.CurrentIndexLevel + 1;
        if (_isTransitioning || _roomTransitionService.IsPlaying)
            return;

        if (!_levelsConfiguration.HasLevel(nextLevelIndex))
        {
            CompleteRunAsync().Forget();
            return;
        }

        TransitToNextLevelAsync(nextLevelIndex).Forget();
    }

    public void Dispose() =>
        _enemiesWaveObserver.RoomCompleted -= HandleRoomCompleted;

    private void HandleRoomCompleted(DefaultEnemiesRoomData roomData)
    {
        int nextLevelIndex = _runtimeDataService.CurrentIndexLevel + 1;
        LevelView currentLevel = _sceneService.GameSceneComponentsService?.CurrentLevel;

        if (!_levelsConfiguration.HasLevel(nextLevelIndex) &&
            currentLevel != null &&
            currentLevel.IsExitRoom(roomData))
            CompleteRunAsync().Forget();
    }

    private async UniTask CompleteRunAsync()
    {
        if (_isRunCompleted || _isTransitioning || _roomTransitionService.IsPlaying)
            return;

        _isTransitioning = true;
        _isRunCompleted = true;

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.4f), ignoreTimeScale: true);
            await _roomTransitionService.Play(() =>
                _gameModeService.Get<RogueLikeStateMachine>()
                    .EnterState<RogueLikeCleanUpState>());
        }
        catch
        {
            _isRunCompleted = false;
            throw;
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    private async UniTask TransitToNextLevelAsync(int nextLevelIndex)
    {
        _isTransitioning = true;

        try
        {
            await _roomTransitionService.Play(() => ReplaceLevel(nextLevelIndex));
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    private UniTask ReplaceLevel(int nextLevelIndex)
    {
        RogueLikeSceneProvider sceneProvider = _sceneService.GameSceneComponentsService;
        CharacterFacade character = _characterProvider.CharacterFacade;

        if (sceneProvider == null)
            throw new InvalidOperationException("RogueLike scene provider is not available.");

        if (character == null)
            throw new InvalidOperationException("Character is not available for level transition.");

        LevelView nextLevel = _levelFactory.CreateLevelView(nextLevelIndex, sceneProvider.LevelSpawnPoint);
        StartRoomData startRoomData;

        try
        {
            startRoomData = GetStartRoomData(nextLevel);
        }
        catch
        {
            UnityEngine.Object.Destroy(nextLevel.gameObject);
            throw;
        }

        LevelView previousLevel = sceneProvider.CurrentLevel;
        if (previousLevel != null)
        {
            previousLevel.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(previousLevel.gameObject);
        }

        _enemiesProvider.ClearEnemies();
        _runtimeDataService.CurrentIndexLevel = nextLevelIndex;
        _runtimeDataService.SetCurrentRoomData(startRoomData);
        sceneProvider.CurrentLevel = nextLevel;

        sceneProvider.NavMeshSurface.RemoveData();
        sceneProvider.NavMeshSurface.BuildNavMesh();

        foreach (RoomDoor roomDoor in startRoomData.RoomDoors)
        {
            if (roomDoor != null)
                roomDoor.Open();
        }

        character.Rigidbody.linearVelocity = Vector3.zero;
        character.Rigidbody.angularVelocity = Vector3.zero;
        character.transform.SetPositionAndRotation(startRoomData.StartPoint.position,
            startRoomData.StartPoint.rotation);

        return UniTask.CompletedTask;
    }

    private static StartRoomData GetStartRoomData(LevelView levelView)
    {
        if (levelView.StartRoom?.RoomData is not StartRoomData startRoomData)
            throw new InvalidOperationException("The next level must contain a start room with StartRoomData.");

        if (startRoomData.StartPoint == null)
            throw new InvalidOperationException("The next level start point is not configured.");

        if (startRoomData.RoomDoors == null)
            throw new InvalidOperationException("The next level start room doors are not configured.");

        return startRoomData;
    }
}

public interface ILevelProgressionService
{
    void TransitToNextLevel();
}
