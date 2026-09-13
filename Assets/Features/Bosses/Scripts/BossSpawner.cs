using System;
using Core;
using Core.Services;
using Features.Bosses.UI;
using Features.Enemies.Scripts;
using Features.Relics.Scripts;
using UnityEngine;

namespace Features.Bosses.Scripts
{
    public sealed class BossSpawner
    {
        private readonly IRogueLikeRuntimeDataService _runtimeDataService;
        private readonly ISceneService<RogueLikeSceneProvider> _sceneService;
        private readonly BossFactory _bossFactory;
        private readonly IEnemiesProvider _enemiesProvider;
        private readonly RelicEventBus _relicEventBus;
        private readonly EnemyRoomObserver _roomObserver;
        private readonly BossHealthUIController _healthUI;

        public BossSpawner(IRogueLikeRuntimeDataService runtimeDataService,
            ISceneService<RogueLikeSceneProvider> sceneService, BossFactory bossFactory,
            IEnemiesProvider enemiesProvider, RelicEventBus relicEventBus,
            EnemyRoomObserver roomObserver, BossHealthUIController healthUI)
        {
            _runtimeDataService = runtimeDataService;
            _sceneService = sceneService;
            _bossFactory = bossFactory;
            _enemiesProvider = enemiesProvider;
            _relicEventBus = relicEventBus;
            _roomObserver = roomObserver;
            _healthUI = healthUI;
        }

        public BossFacade SpawnBoss()
        {
            if (_runtimeDataService.CurrentRoomData is not BossRoomData roomData)
                throw new InvalidOperationException("Bosses can only be spawned in a boss room.");

            LevelView level = _sceneService.GameSceneComponentsService?.CurrentLevel;
            if (level == null)
                throw new InvalidOperationException("Current level view is not available.");

            Room room = GetCurrentRoom(level, roomData);
            BossSpawnPoint point = room.GetComponentInChildren<BossSpawnPoint>();
            if (point == null)
                throw new InvalidOperationException($"Boss spawn point is missing in {room.name}.");
            if (point.BossPrefab == null)
                throw new InvalidOperationException($"Boss prefab is missing at {point.name}.");

            Vector3 position = point.transform.position;
            Vector3 forward = Vector3.ProjectOnPlane(point.transform.forward, Vector3.up);
            Quaternion rotation = forward.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(forward.normalized, Vector3.up)
                : Quaternion.identity;
            BossFacade boss = _bossFactory.Create(point.BossPrefab, position, rotation);
            _enemiesProvider.AddEnemy(boss);
            _relicEventBus.PublishBossSpawned(new RelicBossSpawnEvent(boss, position));
            _healthUI.Show(boss, roomData);
            _roomObserver.FinishEnemySpawning(_enemiesProvider.Count);
            return boss;
        }

        private static Room GetCurrentRoom(LevelView level, BossRoomData roomData)
        {
            for (int i = 0; i < level.Rooms.Count; i++)
            {
                Room room = level.Rooms[i]?.Room;
                if (room != null && ReferenceEquals(room.RoomData, roomData))
                    return room;
            }

            throw new InvalidOperationException(
                $"{level.name} does not contain the current boss room data.");
        }
    }
}
