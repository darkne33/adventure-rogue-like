using System;
using Features.RewardBag;
using UnityEngine;

namespace Features.Relics.Scripts
{
    public sealed class BloodRoomSpawner : IDisposable
    {
        private readonly RelicChestSpawner _chestSpawner;
        private readonly RewardBagSpawner _bagSpawner;
        private readonly RelicEventBus _eventBus;

        public BloodRoomSpawner(RelicChestSpawner chestSpawner, RewardBagSpawner bagSpawner,
            RelicEventBus eventBus)
        {
            _chestSpawner = chestSpawner;
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
            BloodRoomReward reward = roomData.GetReward();
            if ((reward & BloodRoomReward.Bag) != 0)
                SpawnReward(room, roomData, BloodRoomReward.Bag);
            if ((reward & BloodRoomReward.Chest) != 0)
                SpawnReward(room, roomData, BloodRoomReward.Chest);
        }

        private void SpawnReward(Room room, BloodRoomData roomData, BloodRoomReward reward)
        {
            if (roomData.HasSpawned(reward))
                return;

            Transform spawnPoint = roomData.GetReward() == BloodRoomReward.BagAndChest
                ? reward switch
                {
                    BloodRoomReward.Bag => roomData.BagSpawnPoint,
                    BloodRoomReward.Chest => roomData.ChestSpawnPoint,
                    _ => throw new InvalidOperationException("Unknown Blood room reward.")
                }
                : roomData.RewardSpawnPoint;
            if (spawnPoint == null)
                throw new InvalidOperationException($"{room.name} is missing its Blood {reward} spawn point.");

            bool spawned = reward switch
            {
                BloodRoomReward.Bag => _bagSpawner.TrySpawnAt(room, spawnPoint,
                    room.GetComponentInParent<LevelView>(),
                    () => roomData.MarkRewardCollected(BloodRoomReward.Bag)),
                BloodRoomReward.Chest => _chestSpawner.TrySpawnAt(room, spawnPoint, out _),
                _ => throw new InvalidOperationException("Unknown Blood room reward.")
            };

            if (spawned)
                roomData.MarkRewardSpawned(reward);
            else
                Debug.LogWarning($"Could not spawn {reward} reward in {room.name}.");
        }

        private static void HandleChestCollected(RoomData roomData, Room room)
        {
            if (roomData is BloodRoomData bloodRoomData)
                bloodRoomData.MarkRewardCollected(BloodRoomReward.Chest);
        }
    }
}
