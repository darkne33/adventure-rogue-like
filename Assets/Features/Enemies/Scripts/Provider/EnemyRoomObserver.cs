using System;
using System.Collections.Generic;

namespace Features.Enemies.Scripts
{
    public class EnemyRoomObserver
    {
        public int CompletedRooms => _completedRooms.Count;
        public bool IsRoomCompleted { get; private set; }

        private readonly IRogueLikeRuntimeDataService _runtimeDataService;
        private readonly HashSet<DefaultEnemiesRoomData> _completedRooms = new();
        private bool _isEnemySpawningActive;

        public event Action<DefaultEnemiesRoomData> RoomCompleted;

        public EnemyRoomObserver(IRogueLikeRuntimeDataService runtimeDataService)
        {
            _runtimeDataService = runtimeDataService;
        }

        public void Observe(List<EnemyFacade> enemies)
        {
            if (enemies == null)
                throw new ArgumentNullException(nameof(enemies));

            if (enemies.Count == 0 && !_isEnemySpawningActive)
                CompleteCurrentRoom();
        }

        public void StartRoom(bool waitForEnemySpawning = false)
        {
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

        public void CompleteCurrentRoom()
        {
            if (IsRoomCompleted)
                return;

            _isEnemySpawningActive = false;

            DefaultEnemiesRoomData currentRoomData = GetCurrentRoomData();
            if (currentRoomData.RoomDoors == null)
                throw new InvalidOperationException("Room doors are not configured.");

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
                throw new InvalidOperationException(
                    "Enemy rooms can only be observed while an enemy room is active.");

            return currentRoomData;
        }
    }
}
