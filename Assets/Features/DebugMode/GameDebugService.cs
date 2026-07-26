using System;
using Core;
using Cysharp.Threading.Tasks;
using Features.Enemies.Scripts;
using Features.Enemies.Scripts.Level.Scripts;
using Features.Relics.Scripts;
using IngameDebugConsole;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public sealed class GameDebugService : IInitializable, IDisposable
{
    private const string AddExpCommand = "debug.exp";
    private const string AddLevelCommand = "debug.level";
    private const string CompleteRoomCommand = "debug.room.complete";
    private const string RestartRoomCommand = "debug.room.restart";
    private const string RestartGameCommand = "debug.game.restart";
    private const string StatusCommand = "debug.status";
    private const string GiveRelicCommand = "debug.relic.give";
    private const string GiveRandomRelicCommand = "debug.relic.random";
    private const string ClearRelicsCommand = "debug.relic.clear";
    private const string PrintRelicsCommand = "debug.relics";

    private readonly ICharacterLevelService _characterLevelService;
    private readonly CharacterExpConfig _characterExpConfig;
    private readonly IEnemiesProvider _enemiesProvider;
    private readonly EnemyRoomObserver _enemyRoomObserver;
    private readonly IRogueLikeRuntimeDataService _runtimeDataService;
    private readonly IGameModeService _gameModeService;
    private readonly ISceneLoader _sceneLoader;
    private readonly ISceneService<RogueLikeSceneProvider> _sceneService;
    private readonly IRoomTransitionService _roomTransitionService;
    private readonly IPauseService _pauseService;
    private readonly RelicManager _relicManager;
    private readonly RelicPool _relicPool;

    private bool _commandsRegistered;
    private bool _isRestartingGame;

    public GameDebugService(ICharacterLevelService characterLevelService,
        CharacterExpConfig characterExpConfig, IEnemiesProvider enemiesProvider,
        EnemyRoomObserver enemyRoomObserver, IRogueLikeRuntimeDataService runtimeDataService,
        IGameModeService gameModeService, ISceneLoader sceneLoader,
        ISceneService<RogueLikeSceneProvider> sceneService,
        IRoomTransitionService roomTransitionService, IPauseService pauseService,
        RelicManager relicManager, RelicPool relicPool)
    {
        _characterLevelService = characterLevelService;
        _characterExpConfig = characterExpConfig;
        _enemiesProvider = enemiesProvider;
        _enemyRoomObserver = enemyRoomObserver;
        _runtimeDataService = runtimeDataService;
        _gameModeService = gameModeService;
        _sceneLoader = sceneLoader;
        _sceneService = sceneService;
        _roomTransitionService = roomTransitionService;
        _pauseService = pauseService;
        _relicManager = relicManager;
        _relicPool = relicPool;
    }

    public void Initialize()
    {
        if (IsDebugModeAvailable() == false)
            return;

        DebugLogConsole.AddCommand<int, string>(AddExpCommand,
            "Adds experience to the character", AddExperience, "amount");
        DebugLogConsole.AddCommand(AddLevelCommand,
            "Adds enough experience to gain one level", AddLevel);
        DebugLogConsole.AddCommand(CompleteRoomCommand,
            "Defeats all enemies and opens the current room", CompleteRoom);
        DebugLogConsole.AddCommand(RestartRoomCommand,
            "Restarts the current enemy room", RestartRoom);
        DebugLogConsole.AddCommand(RestartGameCommand,
            "Restarts the current run", RestartGame);
        DebugLogConsole.AddCommand(StatusCommand,
            "Shows the current debug gameplay state", GetStatus);
        DebugLogConsole.AddCommand<string, string>(GiveRelicCommand,
            "Gives relic by id", GiveRelic, "id");
        DebugLogConsole.AddCommand(GiveRandomRelicCommand,
            "Gives a random available relic", GiveRandomRelic);
        DebugLogConsole.AddCommand(ClearRelicsCommand,
            "Clears all active relics", ClearRelics);
        DebugLogConsole.AddCommand(PrintRelicsCommand,
            "Prints active relics", PrintRelics);

        _commandsRegistered = true;
    }

    public void Dispose()
    {
        if (_commandsRegistered == false)
            return;

        DebugLogConsole.RemoveCommand(AddExpCommand);
        DebugLogConsole.RemoveCommand(AddLevelCommand);
        DebugLogConsole.RemoveCommand(CompleteRoomCommand);
        DebugLogConsole.RemoveCommand(RestartRoomCommand);
        DebugLogConsole.RemoveCommand(RestartGameCommand);
        DebugLogConsole.RemoveCommand(StatusCommand);
        DebugLogConsole.RemoveCommand(GiveRelicCommand);
        DebugLogConsole.RemoveCommand(GiveRandomRelicCommand);
        DebugLogConsole.RemoveCommand(ClearRelicsCommand);
        DebugLogConsole.RemoveCommand(PrintRelicsCommand);
        _commandsRegistered = false;
    }

    private string AddExperience(int amount)
    {
        if (amount <= 0)
            return "Experience amount must be greater than zero.";

        int previousExperience = _characterLevelService.GetCurrentExp;
        int previousLevel = _characterLevelService.GetLevel;
        _characterLevelService.AddExp(amount);

        return $"EXP: {previousExperience} -> {_characterLevelService.GetCurrentExp}, " +
               $"level: {previousLevel} -> {_characterLevelService.GetLevel}.";
    }

    private string AddLevel()
    {
        if (_characterLevelService.GetLevel >= _characterExpConfig.MaxLevel)
            return $"Character is already at max level {_characterExpConfig.MaxLevel}.";

        int requiredExperience = Math.Max(1,
            _characterLevelService.GetMaxExp - _characterLevelService.GetCurrentExp);
        int previousLevel = _characterLevelService.GetLevel;
        _characterLevelService.AddExp(requiredExperience);

        return $"Level: {previousLevel} -> {_characterLevelService.GetLevel}.";
    }

    private string CompleteRoom()
    {
        string validationError = ValidateRoomCommand(requireEnemies: true);
        if (validationError != null)
            return validationError;

        int defeatedEnemies = _enemiesProvider.DefeatAllEnemies();
        _enemyRoomObserver.CompleteCurrentRoom();

        return $"Room completed. Defeated enemies: {defeatedEnemies}.";
    }

    private string RestartRoom()
    {
        string validationError = ValidateRoomCommand(requireEnemies: false);
        if (validationError != null)
            return validationError;

        int removedEnemies = _enemiesProvider.ClearEnemies();
        _pauseService.CancelPause();
        _enemyRoomObserver.ResetCurrentRoom();

        RogueLikeStateMachine stateMachine = _gameModeService.Get<RogueLikeStateMachine>();
        stateMachine.EnterState<RogueLikeRoomPrepareState>().Forget();

        return $"Room restart scheduled. Removed enemies: {removedEnemies}.";
    }

    private string RestartGame()
    {
        if (_isRestartingGame)
            return "Game restart is already in progress.";

        if (_roomTransitionService.IsPlaying)
            return "Wait until the room transition is complete.";

        _isRestartingGame = true;
        RestartGameAsync().Forget();
        return "Game restart scheduled.";
    }

    private string GetStatus()
    {
        string room = _runtimeDataService.CurrentRoomData?.GetType().Name ?? "none";
        string state = _gameModeService.Get<RogueLikeStateMachine>()?.ActiveState?.Name ?? "none";

        return $"Level {_characterLevelService.GetLevel}, " +
               $"EXP {_characterLevelService.GetCurrentExp}/{_characterLevelService.GetMaxExp}, " +
               $"room {room}, enemies {_enemiesProvider.Count}, state {state}.";
    }

    private string GiveRelic(string id) =>
        _relicManager.GiveRelic(id, _relicPool)
            ? $"Relic '{id}' added. {_relicManager.PrintActiveRelics()}"
            : $"Relic '{id}' was not found or cannot be added.";

    private string GiveRandomRelic()
    {
        RelicDefinition relic = _relicPool.Roll(_relicManager.ActiveRelics);
        if (relic == null)
            return "No available relics in pool.";

        _relicManager.AddRelic(relic);
        return $"Relic '{relic.Id}' added. {_relicManager.PrintActiveRelics()}";
    }

    private string ClearRelics()
    {
        _relicManager.ClearRelics();
        return "Relics cleared.";
    }

    private string PrintRelics() =>
        _relicManager.PrintActiveRelics();

    private string ValidateRoomCommand(bool requireEnemies)
    {
        if (_isRestartingGame)
            return "Game restart is in progress.";

        if (_roomTransitionService.IsPlaying)
            return "Wait until the room transition is complete.";

        if (_runtimeDataService.CurrentRoomData is not DefaultEnemiesRoomData)
            return "The character is not inside an enemy room.";

        if (_gameModeService.Get<RogueLikeStateMachine>() == null)
            return "RogueLike state machine is not available.";

        if (requireEnemies && _enemiesProvider.Count == 0)
            return "The current room has no active enemies yet.";

        return null;
    }

    private async UniTask RestartGameAsync()
    {
        try
        {
            string sceneName = _sceneService.GameSceneComponentsService.gameObject.scene.name;
            _pauseService.CancelPause();
            _characterLevelService.Reset();
            _gameModeService.Remove<RogueLikeStateMachine>();

            if (_sceneLoader.HasActiveScene(sceneName))
            {
                await _sceneLoader.UnloadScene(sceneName);
                await _sceneLoader.LoadSceneFromAddressable(sceneName);
            }
            else
            {
                await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single).ToUniTask();
            }

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
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            _isRestartingGame = false;
        }
    }

    private static bool IsDebugModeAvailable()
    {
#if UNITY_EDITOR
        return true;
#else
        return Debug.isDebugBuild;
#endif
    }
}
