using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace Features.Relics.Scripts
{
    public sealed class OnlyRelicRoomSpawner : IDisposable
    {
        private readonly RelicChestConfiguration _configuration;
        private readonly RelicPool _relicPool;
        private readonly RelicManager _relicManager;
        private readonly RelicChestRollService _rollService;
        private readonly RelicEventBus _eventBus;
        private readonly ICharacterProvider _characterProvider;
        private readonly DiContainer _container;
        private readonly HashSet<Room> _rooms = new();
        private readonly HashSet<Room> _spawnedRooms = new();
        private readonly Dictionary<RelicPickup, Action> _releaseByPickup = new();

        public OnlyRelicRoomSpawner(RelicChestConfiguration configuration, RelicPool relicPool,
            RelicManager relicManager, RelicChestRollService rollService, RelicEventBus eventBus,
            ICharacterProvider characterProvider, DiContainer container)
        {
            _configuration = configuration;
            _relicPool = relicPool;
            _relicManager = relicManager;
            _rollService = rollService;
            _eventBus = eventBus;
            _characterProvider = characterProvider;
            _container = container;
            _eventBus.RoomStarted += HandleRoomStarted;
        }

        public void SetLevel(LevelView level)
        {
            ClearPickups();
            _rooms.Clear();
            _spawnedRooms.Clear();
            if (level == null)
                return;

            foreach (LevelRoomNode node in level.Rooms)
                if (node?.Room?.RoomData is OnlyRelicRoomData)
                    _rooms.Add(node.Room);
        }

        public void Dispose()
        {
            _eventBus.RoomStarted -= HandleRoomStarted;
            ClearPickups();
            _rooms.Clear();
            _spawnedRooms.Clear();
        }

        private void HandleRoomStarted(RelicRoomEvent roomEvent)
        {
            Room room = roomEvent.Room;
            if (room == null || !_rooms.Contains(room) || _spawnedRooms.Contains(room) ||
                roomEvent.RoomData is not OnlyRelicRoomData { IsCompleted: false } roomData)
                return;

            if (roomData.RelicSpawnPoint == null || _configuration.OnlyRelicPickupPrefab == null)
                throw new InvalidOperationException($"{room.name} is missing relic pickup references.");

            List<RelicDefinition> available = _relicPool.GetAvailable(_relicManager.ActiveRelics).ToList();
            if (!_rollService.TryReserveReward(available, _relicManager.ActiveRelics,
                    _configuration, out RelicChestRollPlan rollPlan))
                return;

            bool released = false;
            void ReleaseReward()
            {
                if (released)
                    return;
                released = true;
                _rollService.Finish(rollPlan);
            }

            RelicPickup pickup = null;
            try
            {
                pickup = _container.InstantiatePrefabForComponent<RelicPickup>(
                    _configuration.OnlyRelicPickupPrefab, roomData.RelicSpawnPoint.position,
                    roomData.RelicSpawnPoint.rotation, room.transform);
                RelicPickup spawnedPickup = pickup;
                pickup.Construct(rollPlan.Reward, _configuration, _relicManager, _eventBus,
                    _characterProvider, roomData, room,
                    collectedCallback: roomData.MarkCompleted,
                    destroyedCallback: () =>
                    {
                        ReleaseReward();
                        _releaseByPickup.Remove(spawnedPickup);
                    });
                _releaseByPickup.Add(pickup, ReleaseReward);
                _spawnedRooms.Add(room);
            }
            catch
            {
                ReleaseReward();
                if (pickup != null)
                    UnityEngine.Object.Destroy(pickup.gameObject);
                throw;
            }
        }

        private void ClearPickups()
        {
            foreach (KeyValuePair<RelicPickup, Action> entry in _releaseByPickup.ToArray())
            {
                entry.Value();
                if (entry.Key == null)
                    continue;
                entry.Key.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(entry.Key.gameObject);
            }
            _releaseByPickup.Clear();
        }
    }
}
