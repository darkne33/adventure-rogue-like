using System;
using System.Collections.Generic;
using System.Linq;
using Features.Enemies.Scripts;
using UnityEngine;
using Zenject;

namespace Features.Relics.Scripts
{
    public sealed class RelicChestSpawner
    {
        private readonly ICharacterProvider _characterProvider;
        private readonly RelicChestConfiguration _configuration;
        private readonly LevelsConfiguration _levelsConfiguration;
        private readonly RelicPool _relicPool;
        private readonly RelicManager _relicManager;
        private readonly RelicEventBus _eventBus;
        private readonly DiContainer _container;
        private readonly HashSet<Room> _spawnedRooms = new();
        private readonly List<RelicChest> _activeChests = new();

        public IReadOnlyList<RelicChest> ActiveChests => _activeChests;

        public RelicChestSpawner(ICharacterProvider characterProvider, RelicChestConfiguration configuration,
            LevelsConfiguration levelsConfiguration, RelicPool relicPool, RelicManager relicManager,
            RelicEventBus eventBus, DiContainer container)
        {
            _characterProvider = characterProvider;
            _configuration = configuration;
            _levelsConfiguration = levelsConfiguration;
            _relicPool = relicPool;
            _relicManager = relicManager;
            _eventBus = eventBus;
            _container = container;
        }

        public void SpawnForLevel(LevelView level)
        {
            _spawnedRooms.Clear();
            _activeChests.Clear();
            _eventBus.PublishChestsCleared();

            if (level == null || _configuration.ChestPrefab == null ||
                _configuration.RelicPickupPrefab == null)
                return;

            List<Room> rooms = level.Rooms
                .Where(node => node?.Room?.RoomData is DefaultEnemiesRoomData)
                .Select(node => (Room)node.Room)
                .ToList();
            if (rooms.Count == 0)
                return;

            Shuffle(rooms);
            int chestCount = GetRandomChestCount(rooms.Count);
            if (chestCount <= 0)
                return;

            var excludedRelicIds = new HashSet<string>();
            int spawnedCount = 0;

            for (int index = 0; index < rooms.Count && spawnedCount < chestCount; index++)
            {
                if (rooms[index].RoomData is not DefaultEnemiesRoomData roomData ||
                    _spawnedRooms.Contains(rooms[index]))
                    continue;

                RelicDefinition relic = _relicPool.Roll(_relicManager.ActiveRelics, excludedRelicIds);
                if (relic == null)
                    return;

                excludedRelicIds.Add(relic.Id);
                if (SpawnChest(rooms[index], roomData, relic))
                    spawnedCount++;
            }
        }

        private int GetRandomChestCount(int availableRooms)
        {
            if (availableRooms <= 0)
                return 0;

            int min = Mathf.Min(_configuration.MinChestsPerLevel, _configuration.MaxChestsPerLevel);
            int max = Mathf.Max(_configuration.MinChestsPerLevel, _configuration.MaxChestsPerLevel);

            min = Mathf.Clamp(min, 0, availableRooms);
            max = Mathf.Clamp(max, 0, availableRooms);
            if (max <= min)
                return min;

            return UnityEngine.Random.Range(min, max + 1);
        }

        private bool SpawnChest(Room room, DefaultEnemiesRoomData roomData, RelicDefinition relic)
        {
            if (_spawnedRooms.Contains(room))
                return false;

            if (TryGetGroundPoint(room, out Vector3 groundPoint) == false)
            {
                Debug.LogWarning($"Could not find grounded spawn position for relic chest in {room.name}.");
                return false;
            }

            GameObject chestObject = _container.InstantiatePrefab(_configuration.ChestPrefab,
                groundPoint + Vector3.up * _configuration.ChestSpawnHeight, Quaternion.identity, room.transform);

            RelicChest chest = chestObject.GetComponent<RelicChest>();
            if (chest == null)
                throw new InvalidOperationException(
                    $"{_configuration.ChestPrefab.name} must contain RelicChest component.");

            AlignBottomToGround(chestObject, groundPoint.y);
            _spawnedRooms.Add(room);
            _activeChests.Add(chest);
            chest.Construct(relic, _configuration, _relicManager, _eventBus, _characterProvider,
                _container, roomData, room);
            _eventBus.PublishChestSpawned(roomData, room, chestObject.transform.position);
            return true;
        }

        private bool TryGetGroundPoint(Room room, out Vector3 groundPoint)
        {
            int attempts = Mathf.Max(1, _configuration.ChestSpawnAttempts);
            List<Collider> groundColliders = GetGroundColliders(room);

            if (TryCreateGroundSpawnBounds(groundColliders, out Bounds spawnBounds))
            {
                for (int attempt = 0; attempt < attempts; attempt++)
                {
                    Vector3 candidate = GetRandomSpawnCandidate(spawnBounds);
                    if (TryProjectToGround(room, candidate, out groundPoint) &&
                        IsObstacleFree(groundPoint) &&
                        IsAwayFromDoors(room, groundPoint))
                    {
                        return true;
                    }
                }

                if (TryProjectToGround(room, spawnBounds.center, out groundPoint) &&
                    IsObstacleFree(groundPoint) &&
                    IsAwayFromDoors(room, groundPoint))
                {
                    return true;
                }
            }

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                Vector3 candidate = groundColliders.Count > 0
                    ? GetRandomSpawnCandidate(groundColliders)
                    : GetRandomSpawnCandidate(room);

                if (TryProjectToGround(room, candidate, out groundPoint) &&
                    IsObstacleFree(groundPoint) &&
                    IsAwayFromDoors(room, groundPoint))
                {
                    return true;
                }
            }

            foreach (Collider groundCollider in groundColliders.OrderByDescending(GetHorizontalArea))
            {
                if (TryProjectToGround(room, groundCollider.bounds.center, out groundPoint) &&
                    IsObstacleFree(groundPoint) &&
                    IsAwayFromDoors(room, groundPoint))
                {
                    return true;
                }
            }

            groundPoint = Vector3.zero;
            return false;
        }

        private List<Collider> GetGroundColliders(Room room)
        {
            var groundColliders = new List<Collider>();
            LayerMask groundLayer = GetGroundLayerMask();
            Collider[] colliders = room.GetComponentsInChildren<Collider>(false);

            foreach (Collider collider in colliders)
            {
                if (collider == null || collider.isTrigger ||
                    ContainsLayer(groundLayer, collider.gameObject.layer) == false)
                    continue;

                groundColliders.Add(collider);
            }

            return groundColliders;
        }

        private bool TryCreateGroundSpawnBounds(IReadOnlyList<Collider> groundColliders, out Bounds spawnBounds)
        {
            if (groundColliders.Count == 0)
            {
                spawnBounds = default;
                return false;
            }

            spawnBounds = groundColliders[0].bounds;
            for (int index = 1; index < groundColliders.Count; index++)
                spawnBounds.Encapsulate(groundColliders[index].bounds);

            float padding = CalculateSpawnPadding(spawnBounds);
            if (padding <= 0f)
                return true;

            spawnBounds.Expand(new Vector3(-padding * 2f, 0f, -padding * 2f));
            return spawnBounds.size.x > Mathf.Epsilon && spawnBounds.size.z > Mathf.Epsilon;
        }

        private float CalculateSpawnPadding(Bounds bounds)
        {
            float requestedPadding = Mathf.Max(5f, _configuration.ObstacleCheckRadius * 4f);
            float maxPadding = Mathf.Min(bounds.extents.x, bounds.extents.z) - 0.5f;
            return Mathf.Clamp(requestedPadding, 0f, Mathf.Max(0f, maxPadding));
        }

        private Vector3 GetRandomSpawnCandidate(Bounds bounds) =>
            new(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                bounds.max.y,
                UnityEngine.Random.Range(bounds.min.z, bounds.max.z));

        private Vector3 GetRandomSpawnCandidate(IReadOnlyList<Collider> groundColliders)
        {
            Collider groundCollider = groundColliders[UnityEngine.Random.Range(0, groundColliders.Count)];
            Bounds bounds = groundCollider.bounds;
            float margin = Mathf.Max(0.05f, _configuration.ObstacleCheckRadius);
            float minX = bounds.min.x + margin;
            float maxX = bounds.max.x - margin;
            float minZ = bounds.min.z + margin;
            float maxZ = bounds.max.z - margin;

            if (minX > maxX)
            {
                minX = bounds.min.x;
                maxX = bounds.max.x;
            }

            if (minZ > maxZ)
            {
                minZ = bounds.min.z;
                maxZ = bounds.max.z;
            }

            return new Vector3(
                UnityEngine.Random.Range(minX, maxX),
                bounds.max.y,
                UnityEngine.Random.Range(minZ, maxZ));
        }

        private Vector3 GetRandomSpawnCandidate(Room room)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle *
                             Mathf.Max(0f, _configuration.ChestRoomOffsetRadius);
            Vector3 localPosition = new(offset.x, 0f, offset.y);
            return room.transform.TransformPoint(localPosition);
        }

        private bool TryProjectToGround(Room room, Vector3 position, out Vector3 groundPoint)
        {
            Vector3 rayOrigin = position + Vector3.up * _configuration.GroundRayStartHeight;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit,
                    _configuration.GroundRayDistance, GetGroundLayerMask(), QueryTriggerInteraction.Ignore) == false)
            {
                groundPoint = Vector3.zero;
                return false;
            }

            if (hit.collider == null || hit.collider.transform.IsChildOf(room.transform) == false ||
                hit.normal.y < 0.75f)
            {
                groundPoint = Vector3.zero;
                return false;
            }

            groundPoint = hit.point;
            return true;
        }

        private bool IsObstacleFree(Vector3 position)
        {
            if (_levelsConfiguration.ObstacleLayer.value == 0 || _configuration.ObstacleCheckRadius <= 0f)
                return true;

            Vector3 checkPosition = position + Vector3.up * _configuration.ObstacleCheckHeight;
            Collider[] colliders = Physics.OverlapSphere(checkPosition, _configuration.ObstacleCheckRadius,
                _levelsConfiguration.ObstacleLayer, QueryTriggerInteraction.Ignore);
            return colliders.Length == 0;
        }

        private bool IsAwayFromDoors(Room room, Vector3 position)
        {
            const float MinDoorDistance = 8f;

            if (room.RoomData?.RoomDoors == null)
                return true;

            float minDoorDistanceSqr = MinDoorDistance * MinDoorDistance;
            foreach (RoomDoor door in room.RoomData.RoomDoors)
            {
                if (door == null)
                    continue;

                Vector3 offset = door.transform.position - position;
                offset.y = 0f;
                if (offset.sqrMagnitude < minDoorDistanceSqr)
                    return false;
            }

            return true;
        }

        private LayerMask GetGroundLayerMask() =>
            _levelsConfiguration.GroundLayer.value == 0
                ? Physics.DefaultRaycastLayers
                : _levelsConfiguration.GroundLayer;

        private static bool ContainsLayer(LayerMask layerMask, int layer) =>
            (layerMask.value & (1 << layer)) != 0;

        private static float GetHorizontalArea(Collider collider) =>
            collider.bounds.size.x * collider.bounds.size.z;

        private static void AlignBottomToGround(GameObject chestObject, float groundY)
        {
            Renderer[] renderers = chestObject.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);

            float yOffset = groundY - bounds.min.y;
            chestObject.transform.position += Vector3.up * yOffset;
        }

        private static void Shuffle<T>(IList<T> items)
        {
            for (int index = 0; index < items.Count - 1; index++)
            {
                int swapIndex = UnityEngine.Random.Range(index, items.Count);
                (items[index], items[swapIndex]) = (items[swapIndex], items[index]);
            }
        }
    }
}
