using System;
using Features.RewardBag;
using UnityEngine;

namespace Features.Relics.Scripts
{
    public sealed class BloodRoomSpawner : IDisposable
    {
        private readonly RelicChestSpawner _chestSpawner;
        private readonly OnlyRelicRoomSpawner _relicSpawner;
        private readonly RewardBagSpawner _bagSpawner;
        private readonly RelicEventBus _eventBus;

        public BloodRoomSpawner(RelicChestSpawner chestSpawner, OnlyRelicRoomSpawner relicSpawner,
            RewardBagSpawner bagSpawner, RelicEventBus eventBus)
        {
            _chestSpawner = chestSpawner;
            _relicSpawner = relicSpawner;
            _bagSpawner = bagSpawner;
            _eventBus = eventBus;
            _eventBus.RoomStarted += HandleRoomStarted;
            _eventBus.ChestCollected += HandleChestCollected;
        }

        public void Dispose()
        {
            _eventBus.RoomStarted -= HandleRoomStarted;
            _eventBus.ChestCollected -= HandleChestCollected;
        }

        private void HandleRoomStarted(RelicRoomEvent roomEvent)
        {
            if (roomEvent.RoomData is not BloodRoomData { HasSpawnedReward: false, IsCompleted: false } roomData ||
                roomEvent.Room == null)
                return;

            Room room = roomEvent.Room;
            Transform spawnPoint = roomData.RewardSpawnPoint;
            if (spawnPoint == null)
                throw new InvalidOperationException($"{room.name} is missing its Blood reward spawn point.");

            bool spawned = roomData.GetReward() switch
            {
                BloodRoomReward.Bag => _bagSpawner.TrySpawnAt(room, spawnPoint,
                    room.GetComponentInParent<LevelView>(), roomData.MarkCompleted),
                BloodRoomReward.Chest => _chestSpawner.TrySpawnAt(room, spawnPoint, out _),
                BloodRoomReward.Relic => _relicSpawner.TrySpawnAt(room, spawnPoint, roomData.MarkCompleted),
                _ => throw new InvalidOperationException("Unknown Blood room reward.")
            };

            if (spawned)
                roomData.MarkRewardSpawned();
            else
                Debug.LogWarning($"Could not spawn {roomData.GetReward()} reward in {room.name}.");
        }

        private static void HandleChestCollected(RoomData roomData, Room room)
        {
            if (roomData is BloodRoomData bloodRoomData)
                bloodRoomData.MarkCompleted();
        }
    }
}
