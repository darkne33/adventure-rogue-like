using System.Collections.Generic;
using Core;
using Cysharp.Threading.Tasks;

namespace Features.Enemies.Scripts
{
    public class EnemiesWaveObserver
    {
        public int CurrentWave { get; private set; }
        public bool IsRoomCompleted { get; private set; }

        private readonly IRogueLikeRuntimeDataService _runtimeDataService;
        private readonly IGameModeService _gameModeService;

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

            if (enemies.Count == 0)
                CompleteCurrentWave();
        }

        public void StartRoom()
        {
            CurrentWave = 0;
            IsRoomCompleted = false;
        }

        public void CompleteCurrentWave()
        {
            DefaultEnemiesRoomData currentRoomData = GetCurrentRoomData();
            if (IsRoomCompleted)
                return;

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

            DefaultEnemiesRoomData currentRoomData = GetCurrentRoomData();
            if (currentRoomData.RoomDoors == null)
                throw new System.InvalidOperationException("Room doors are not configured.");

            if (currentRoomData.EnemyWavesConfiguration != null &&
                currentRoomData.EnemyWavesConfiguration.Length > 0)
                CurrentWave = currentRoomData.EnemyWavesConfiguration.Length - 1;

            IsRoomCompleted = true;

            foreach (RoomDoor roomDoor in currentRoomData.RoomDoors)
                roomDoor.Open();
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
