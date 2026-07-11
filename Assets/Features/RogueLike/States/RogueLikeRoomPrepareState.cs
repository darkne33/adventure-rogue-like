using System;
using System.Threading;
using CustomPackages.Package.StateMachine.States;
using Cysharp.Threading.Tasks;
using Features.Enemies.Scripts;
using UI;
using UnityEngine;

namespace Core
{
    public class RogueLikeRoomPrepareState : State
    {
        private readonly IRogueLikeRuntimeDataService _rogueLikeRuntimeDataService;
        private readonly EnemiesWaveObserver _enemiesWaveObserver;
        private readonly EnemySpawner _enemySpawner;
        private readonly ICharacterProvider _characterProvider;
        private readonly IEnemiesProvider _enemiesProvider;
        private readonly IPanelService _panelService;
        private readonly LevelsConfiguration _levelsConfiguration;

        public RogueLikeRoomPrepareState(IRogueLikeRuntimeDataService rogueLikeRuntimeDataService,
            EnemiesWaveObserver enemiesWaveObserver, EnemySpawner enemySpawner,
            ICharacterProvider characterProvider, IEnemiesProvider enemiesProvider,
            IPanelService panelService, LevelsConfiguration levelsConfiguration)
        {
            _rogueLikeRuntimeDataService = rogueLikeRuntimeDataService;
            _enemiesWaveObserver = enemiesWaveObserver;
            _enemySpawner = enemySpawner;
            _characterProvider = characterProvider;
            _enemiesProvider = enemiesProvider;
            _panelService = panelService;
            _levelsConfiguration = levelsConfiguration;
        }

        public override async UniTask Enter(CancellationToken cts)
        {
            if (_rogueLikeRuntimeDataService.CurrentRoomData is not DefaultEnemiesRoomData currentRoomData)
                throw new InvalidOperationException(
                    "Room prepare state supports only default enemies room data.");

            if (currentRoomData.RoomDoors == null)
                throw new InvalidOperationException("Room doors are not configured.");

            if (_enemiesWaveObserver.RestoreCompletedRoom())
                return;

            _enemiesWaveObserver.StartRoom(waitForEnemySpawning: true);
            await _enemySpawner.LoadEnemyPrefabs(cts);

            _enemySpawner.TrySpawnEnemies(_characterProvider.CharacterFacade,
                _enemiesWaveObserver.CurrentWave);

            RunTimedAdditionalSpawning(currentRoomData, cts).Forget();
        }

        private async UniTask RunTimedAdditionalSpawning(DefaultEnemiesRoomData roomData,
            CancellationToken cancellationToken)
        {
            EnemyTimedSpawnScalingConfiguration scalingConfiguration =
                _levelsConfiguration.GetEnemyTimedSpawnScalingConfiguration();
            float duration = scalingConfiguration.GetDuration(roomData.TimedSpawnDuration,
                _enemiesWaveObserver.CompletedRooms);
            if (duration <= 0f)
            {
                _enemiesWaveObserver.FinishEnemySpawning(_enemiesProvider.Count);
                return;
            }

            float spawnInterval = Mathf.Max(0.1f, roomData.AdditionalSpawnInterval);
            float spawnTimer = spawnInterval;
            float remainingTime = duration;
            int shownSeconds = Mathf.CeilToInt(remainingTime);
            bool timerCompleted = false;
            RoomTimerView timerView = null;

            try
            {
                CharacterPanel panel = _panelService
                    .GetPanelPresenter<CharacterPanelPresenter>(PanelName.CharacterPanel)
                    .Panel;
                timerView = panel.RoomTimerView;
                timerView?.Show(shownSeconds);

                while (remainingTime > 0f)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                    float deltaTime = Time.deltaTime;
                    remainingTime -= deltaTime;
                    spawnTimer -= deltaTime;

                    if (spawnTimer <= 0f && remainingTime > 0f)
                    {
                        _enemySpawner.TrySpawnAdditionalEnemies(_characterProvider.CharacterFacade,
                            _enemiesWaveObserver.CurrentWave, roomData.AdditionalEnemiesPerSpawn);
                        spawnTimer += spawnInterval;
                    }

                    int seconds = Mathf.Max(0, Mathf.CeilToInt(remainingTime));
                    if (seconds == shownSeconds)
                        continue;

                    shownSeconds = seconds;
                    timerView?.UpdateValue(shownSeconds);
                }

                timerCompleted = true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                timerCompleted = true;
            }
            finally
            {
                timerView?.Hide();

                if (timerCompleted)
                    _enemiesWaveObserver.FinishEnemySpawning(_enemiesProvider.Count);
            }
        }
    }
}
