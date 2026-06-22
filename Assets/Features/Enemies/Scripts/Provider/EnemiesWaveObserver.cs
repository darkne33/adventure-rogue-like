using System;
using System.Collections.Generic;
using Core;
using Cysharp.Threading.Tasks;

namespace Features.Enemies.Scripts
{
    public class EnemiesWaveObserver
    {
        public int CurrentWave { get; private set; }
        public int CompletedRooms => _completedRooms.Count;
        public bool IsRoomCompleted { get; private set; }

        private readonly IRogueLikeRuntimeDataService _runtimeDataService;
        private readonly IGameModeService _gameModeService;
        private readonly HashSet<DefaultEnemiesRoomData> _completedRooms = new();
        private bool _isEnemySpawningActive;

        public event Action<DefaultEnemiesRoomData> RoomCompleted;

        public EnemiesWaveObserver(IRogueLikeRuntimeDataService runtimeDataService,
            IGameModeService gameModeService)
        {
            _runtimeDataService = runtimeDataService;
            _gameModeService = gameModeService;
        }

        public void Observe(List<EnemyFacade> enemies)
        {
            if (enemies == null)
                throw new System.ArgumentNullException(nameof(enemies));

            if (enemies.Count == 0 && _isEnemySpawningActive == false)
                CompleteCurrentRoom();
        }

        public void StartRoom(bool waitForEnemySpawning = false)
        {
            CurrentWave = 0;
            IsRoomCompleted = false;
            _isEnemySpawningActive = waitForEnemySpawning;
        }

        public void FinishEnemySpawning(int activeEnemyCount)
        {
            _isEnemySpawningActive = false;

            if (activeEnemyCount <= 0)
                CompleteCurrentRoom();
        }

        public bool RestoreCompletedRoom()
        {
            DefaultEnemiesRoomData currentRoomData = GetCurrentRoomData();
            if (!currentRoomData.IsCompleted)
                return false;

            CurrentWave = currentRoomData.EnemyWavesConfiguration == null
                ? 0
                : System.Math.Max(0, currentRoomData.EnemyWavesConfiguration.Length - 1);
            IsRoomCompleted = true;
            _isEnemySpawningActive = false;

            OpenRoomDoors(currentRoomData);
            return true;
        }

        public void ResetCurrentRoom()
        {
            GetCurrentRoomData().ResetProgress();
            StartRoom();
        }

        public void CompleteCurrentWave()
        {
            DefaultEnemiesRoomData currentRoomData = GetCurrentRoomData();
            if (IsRoomCompleted)
                return;

            _isEnemySpawningActive = false;

            if (currentRoomData.EnemyWavesConfiguration == null ||
                currentRoomData.EnemyWavesConfiguration.Length == 0)
                throw new System.InvalidOperationException(
                    "Enemy waves are not configured for the current room.");

            int lastCurrentWaveIndex = currentRoomData.EnemyWavesConfiguration.Length - 1;
            if (CurrentWave < lastCurrentWaveIndex)
            {
                CurrentWave++;
                RogueLikeStateMachine stateMachine =
                    _gameModeService.Get<RogueLikeStateMachine>();
                stateMachine.EnterState<RogueLikeSpawnEnemyWaveState>().Forget();
                return;
            }

            CompleteCurrentRoom();
        }

        public void CompleteCurrentRoom()
        {
            if (IsRoomCompleted)
                return;

            _isEnemySpawningActive = false;

            DefaultEnemiesRoomData currentRoomData = GetCurrentRoomData();
            if (currentRoomData.RoomDoors == null)
                throw new System.InvalidOperationException("Room doors are not configured.");

            if (currentRoomData.EnemyWavesConfiguration != null &&
                currentRoomData.EnemyWavesConfiguration.Length > 0)
                CurrentWave = currentRoomData.EnemyWavesConfiguration.Length - 1;

            currentRoomData.MarkCompleted();
            IsRoomCompleted = true;

            OpenRoomDoors(currentRoomData);

            if (_completedRooms.Add(currentRoomData))
                RoomCompleted?.Invoke(currentRoomData);
        }

        private static void OpenRoomDoors(DefaultEnemiesRoomData currentRoomData)
        {
            foreach (RoomDoor roomDoor in currentRoomData.RoomDoors)
            {
                if (roomDoor != null)
                    roomDoor.Open();
            }
        }

        private DefaultEnemiesRoomData GetCurrentRoomData()
        {
            if (_runtimeDataService.CurrentRoomData is not DefaultEnemiesRoomData currentRoomData)
                throw new System.InvalidOperationException(
                    "Enemy waves can only be observed in a default enemies room.");

            return currentRoomData;
        }
    }
}
