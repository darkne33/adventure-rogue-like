using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Zenject;

public class LevelView : MonoBehaviour
{
    public const float RoomWorldSize = 320f;
    private const string WallLayerName = "Wall";
    private const string NotWalkableAreaName = "Not Walkable";
    private static readonly RoomDirection[] CardinalDirections =
    {
        RoomDirection.Up, RoomDirection.Down, RoomDirection.Left, RoomDirection.Right
    };

    [SerializeField] private LevelRoomNode[] _rooms;

    [Header("Combat Room Variants")]
    [Tooltip("Small layouts for low enemy counts. Variants are picked without repetition until the pool is exhausted.")]
    [SerializeField] private Room[] _smallEnemyRooms = Array.Empty<Room>();
    [Tooltip("Medium layouts for larger enemy groups. Enemy Room Scaling Configuration controls the size threshold.")]
    [SerializeField] private Room[] _mediumEnemyRooms = Array.Empty<Room>();

    [Header("Key Room Spawning")]
    [Tooltip("Prefab spawned at the point configured in DefaultEnemiesRoomData.")]
    [SerializeField] private GameObject _keyRoomPrefab;
    [Tooltip("Chance for each eligible combat room after the start-progress threshold. " +
             "The last remaining candidate is forced if no Key_Room has spawned.")]
    [SerializeField, Range(0f, 100f)] private float _keyRoomSpawnChancePercent = 50f;
    [Tooltip("Percent of unique non-start rooms that must be visited before spawning can begin.")]
    [SerializeField, Range(0f, 100f)] private float _keyRoomStartProgressPercent = 50f;
    [Tooltip("Number of newly visited rooms skipped after a successful Key_Room spawn.")]
    [SerializeField, Min(0)] private int _roomsBetweenKeyRoomSpawns = 2;

    public Room StartRoomPrefab => GetRoomNode(RoomType.Start).RoomPrefab;
    public Room StartRoom => GetRoomNode(RoomType.Start).Room;
    public Vector2Int StartRoomGridPosition => GetRoomNode(RoomType.Start).GridPosition;
    public IReadOnlyList<LevelRoomNode> Rooms => _rooms;
    public bool HasDroppedKeyReward { get; private set; }

    private readonly HashSet<RoomData> _keyRoomVisitedRooms = new();
    private Dictionary<Vector2Int, int> _combatDepths;
    private DiContainer _container;
    private bool _isInitialized;
    private int _visitedNonStartRooms;
    private int _spawnedKeyRooms;
    private int _roomsUntilNextKeyRoomChance;
    private int _spawnedRewardBags;
    private int _guaranteedKeyRewardBagNumber;

    public void Configure(LevelRoomNode[] rooms)
    {
        _rooms = rooms;
        _combatDepths = null;
        _isInitialized = false;
    }

    public void Initialize(DiContainer container, bool hasNextLevel,
        EnemyRoomScalingConfiguration roomBalance = null, int combatProgressOffset = 0)
    {
        if (_isInitialized)
            return;

        if (container == null)
            throw new ArgumentNullException(nameof(container));

        _container = container;
        MaterializeRooms(container, roomBalance, combatProgressOffset);
        ResolveAuthoredDoors();
        ResetRoomProgress();

        Dictionary<Vector2Int, Room> roomsByPosition = ValidateAndBuildRoomMap();
        ValidateKeyRoomConfiguration(useRuntimeRooms: true);
        ValidateRoomDoors(roomsByPosition.Values);
        ValidateRequiredDoors(roomsByPosition, hasNextLevel);
        ValidateConnectivity(roomsByPosition);
        ResetDoors(roomsByPosition.Values);
        ConnectAdjacentRooms(roomsByPosition);
        ConfigureStartPointRotation();
        _combatDepths = BuildCombatDepths();
        ConfigureLevelExit(hasNextLevel);
        ResetKeyRoomSpawnState();
        ResetKeyRewardState();

        _isInitialized = true;
    }

    public void ResolveRoomReferences()
    {
        if (_rooms == null || _rooms.Length == 0)
            throw new InvalidOperationException($"{name} does not contain room nodes.");

        foreach (LevelRoomNode roomNode in _rooms)
        {
            if (roomNode == null)
                throw new InvalidOperationException($"{name} contains a missing room node.");
            if (roomNode.RoomPrefab == null)
                throw new InvalidOperationException($"{name} contains a missing room prefab.");

            roomNode.Bind(roomNode.RoomPrefab);
            PositionEmbeddedRoom(roomNode);
        }
    }

    [ContextMenu("Validate Level Authoring")]
    public void ValidateAuthoring()
    {
        if (_rooms == null || _rooms.Length == 0)
            throw new InvalidOperationException($"{name} does not contain room nodes.");

        var sourcesByPosition = new Dictionary<Vector2Int, Room>();
        int startCount = 0;
        int exitCount = 0;

        foreach (LevelRoomNode roomNode in _rooms)
        {
            if (roomNode == null || roomNode.RoomPrefab == null)
                throw new InvalidOperationException($"{name} contains a missing room prefab.");

            ValidateSourceRoomData(roomNode);

            if (!sourcesByPosition.TryAdd(roomNode.GridPosition, roomNode.RoomPrefab))
                throw new InvalidOperationException(
                    $"{name} contains more than one room at {roomNode.GridPosition}.");

            if (roomNode.Type == RoomType.Start)
                startCount++;
            if (roomNode.Type is RoomType.Exit or RoomType.Boss)
                exitCount++;
        }

        ValidateKeyRoomConfiguration(useRuntimeRooms: false);
        ValidateTopology(sourcesByPosition, startCount, exitCount);
        ValidateConnectivity(_rooms.ToDictionary(
            node => node.GridPosition, GetRoomDirections));
    }

    private void MaterializeRooms(DiContainer container,
        EnemyRoomScalingConfiguration roomBalance, int combatProgressOffset)
    {
        if (_rooms == null || _rooms.Length == 0)
            throw new InvalidOperationException($"{name} does not contain room nodes.");

        if (roomBalance != null)
            _combatDepths = BuildCombatDepths();
        var usedVariants = new HashSet<Room>();
        var nodesByPosition = _rooms.Where(node => node != null)
            .ToDictionary(node => node.GridPosition);

        foreach (LevelRoomNode roomNode in _rooms)
        {
            if (roomNode == null)
                throw new InvalidOperationException($"{name} contains a missing room node.");
            if (roomNode.RoomPrefab == null)
                throw new InvalidOperationException($"{name} contains a missing room prefab.");

            Room source = roomNode.RoomPrefab;
            HashSet<RoomDirection> requiredDirections =
                GetRequiredDirections(roomNode, nodesByPosition);
            if (roomBalance != null && (roomNode.Type is RoomType.Enemy or RoomType.Exit) &&
                source.RoomData is not BossRoomData &&
                !IsRoomOwnedByLevel(source) && !IsRoomOwnedByLevel(roomNode.Room))
            {
                int roomIndex = combatProgressOffset +
                                Mathf.Max(0, _combatDepths[roomNode.GridPosition] - 1);
                Room[] variants = roomBalance.UsesSmallRoom(roomIndex)
                    ? _smallEnemyRooms
                    : _mediumEnemyRooms;
                source = SelectRoomVariant(source, variants, usedVariants,
                    requiredDirections);
            }

            Room room = MaterializeRoom(container, source,
                roomNode.Room, roomNode.GridPosition, "room");
            ApplyRoomRotation(room, GetMatchingRotation(room, requiredDirections));

            if (roomNode.Type is RoomType.Enemy or RoomType.Exit)
            {
                RoomDoor[] authoredDoors = room.RoomData?.RoomDoors;
                var enemiesRoomData = room.RoomData as DefaultEnemiesRoomData ??
                                      new DefaultEnemiesRoomData
                                      {
                                          RoomDoors = authoredDoors
                                      };
                enemiesRoomData.Configure(roomNode.EnemySettings);
                room.SetRoomData(enemiesRoomData);
            }
            else if (roomNode.Type == RoomType.Reward && room.RoomData is not RewardRoomData)
            {
                RoomDoor[] authoredDoors = room.RoomData?.RoomDoors;
                room.SetRoomData(new RewardRoomData
                {
                    RoomDoors = authoredDoors
                });
            }
            else if (roomNode.Type == RoomType.Shop && room.RoomData is not ShopRoomData)
            {
                RoomDoor[] authoredDoors = room.RoomData?.RoomDoors;
                room.SetRoomData(new ShopRoomData
                {
                    RoomDoors = authoredDoors
                });
            }

            roomNode.Bind(room);
        }
    }

    private static Room SelectRoomVariant(Room authoredRoom, Room[] variants,
        HashSet<Room> usedVariants, IReadOnlyCollection<RoomDirection> requiredDirections)
    {
        if (variants == null || variants.Length == 0)
            return authoredRoom;

        bool requiresKeyRoom = authoredRoom.RoomData is
            DefaultEnemiesRoomData { CanSpawnKeyRoom: true };
        var compatible = new List<Room>();
        foreach (Room variant in variants)
        {
            if (variant == null || variant.RoomData is BossRoomData ||
                variant.RoomData is not DefaultEnemiesRoomData data ||
                (requiresKeyRoom && (!data.CanSpawnKeyRoom || data.KeyRoomSpawnPoint == null)))
                continue;

            // A variant may have a different door layout if a quarter-turn rotation
            // fits every connection required by this grid cell.
            if (TryMatchRotation(variant, requiredDirections, out _) &&
                !compatible.Contains(variant))
                compatible.Add(variant);
        }

        if (compatible.Count == 0)
            return authoredRoom;

        List<Room> available = compatible.Where(room => !usedVariants.Contains(room)).ToList();
        if (available.Count == 0)
        {
            foreach (Room variant in compatible)
                usedVariants.Remove(variant);
            available = compatible;
        }

        Room selected = available[UnityEngine.Random.Range(0, available.Count)];
        usedVariants.Add(selected);
        return selected;
    }

    private void ResolveAuthoredDoors()
    {
        foreach (Room room in _rooms.Select(roomNode => roomNode.Room))
        {
            if (room?.RoomData == null)
                throw new InvalidOperationException($"{room?.name ?? name} does not have room data.");

            RoomDoor[] authoredDoors = room.GetComponentsInChildren<RoomDoor>(true)
                .OrderBy(door => door.AuthoredDirection)
                .ToArray();
            if (authoredDoors.Length == 0)
                throw new InvalidOperationException(
                    $"{room.name} does not contain authored RoomDoor objects.");

            RoomDoor[] configuredDoors = room.RoomData.RoomDoors;
            if (configuredDoors == null || configuredDoors.Length == 0)
                throw new InvalidOperationException(
                    $"{room.name} does not have active doors configured in RoomData.");

            var configuredDirections = new HashSet<RoomDirection>();
            var resolvedDoors = new RoomDoor[configuredDoors.Length];
            for (int i = 0; i < configuredDoors.Length; i++)
            {
                RoomDoor configuredDoor = configuredDoors[i];
                if (configuredDoor == null)
                    throw new InvalidOperationException(
                        $"{room.name} contains a missing active door in RoomData.");
                if (!configuredDirections.Add(configuredDoor.AuthoredDirection))
                    throw new InvalidOperationException(
                        $"{room.name} contains duplicate {configuredDoor.AuthoredDirection} active doors.");

                RoomDoor[] matches = authoredDoors
                    .Where(door => door.AuthoredDirection == configuredDoor.AuthoredDirection)
                    .ToArray();
                if (matches.Length != 1)
                    throw new InvalidOperationException(
                        $"{room.name} must contain exactly one authored {configuredDoor.AuthoredDirection} door.");

                resolvedDoors[i] = matches[0];
            }

            room.RoomData.RoomDoors = resolvedDoors;
        }
    }

    private T MaterializeRoom<T>(DiContainer container, T source, T current,
        Vector2Int gridPosition, string role) where T : Room
    {
        T instance;
        if (current != null && IsRoomOwnedByLevel(current))
        {
            instance = current;
        }
        else if (IsRoomOwnedByLevel(source))
        {
            instance = source;
        }
        else
        {
            instance = container.InstantiatePrefabForComponent<T>(source, transform);
            if (instance == null)
                throw new InvalidOperationException(
                    $"Could not instantiate {role} prefab {source.name}.");
        }

        if (instance.transform.parent != transform)
            instance.transform.SetParent(transform, false);

        instance.transform.SetLocalPositionAndRotation(
            ToWorldPosition(gridPosition), Quaternion.identity);
        return instance;
    }

    private Dictionary<Vector2Int, Room> ValidateAndBuildRoomMap()
    {
        var roomsByPosition = new Dictionary<Vector2Int, Room>();
        var roomInstances = new HashSet<Room>();

        int startCount = 0;
        int exitCount = 0;
        foreach (LevelRoomNode roomNode in _rooms)
        {
            if (roomNode?.Room == null)
                throw new InvalidOperationException($"{name} contains a missing room instance.");

            ValidateRuntimeRoomData(roomNode);

            if (!roomInstances.Add(roomNode.Room))
                throw new InvalidOperationException(
                    $"{name} assigns {roomNode.Room.name} to more than one room node.");

            if (!roomsByPosition.TryAdd(roomNode.GridPosition, roomNode.Room))
                throw new InvalidOperationException(
                    $"{name} contains more than one room at {roomNode.GridPosition}.");

            if (roomNode.Type == RoomType.Start)
                startCount++;
            if (roomNode.Type is RoomType.Exit or RoomType.Boss)
                exitCount++;
        }

        ValidateTopology(roomsByPosition, startCount, exitCount);
        return roomsByPosition;
    }

    private void ValidateTopology<T>(IReadOnlyDictionary<Vector2Int, T> roomsByPosition,
        int startCount, int exitCount)
    {
        if (startCount != 1)
            throw new InvalidOperationException(
                $"{name} must contain exactly one start room.");
        if (exitCount != 1)
            throw new InvalidOperationException(
                $"{name} must contain exactly one final room (Exit or Boss).");

        LevelRoomNode exitNode = GetExitRoomNode();
        Vector2Int destination =
            exitNode.GridPosition + exitNode.LevelExitDirection.ToGridOffset();
        if (roomsByPosition.ContainsKey(destination))
            throw new InvalidOperationException(
                $"{name} level exit at {exitNode.GridPosition} points into another room.");
    }

    private static void ValidateSourceRoomData(LevelRoomNode roomNode)
    {
        RoomData roomData = roomNode.RoomPrefab.RoomData;
        switch (roomNode.Type)
        {
            case RoomType.Start when roomData is StartRoomData startRoomData:
                if (startRoomData.StartPoint == null)
                    throw new InvalidOperationException(
                        $"{roomNode.RoomPrefab.name} does not have a start point.");
                return;
            case RoomType.Start:
                throw new InvalidOperationException(
                    $"{roomNode.RoomPrefab.name} must contain StartRoomData.");
            case RoomType.Reward:
                return;
            case RoomType.Shop:
                return;
            case RoomType.Boss:
                if (roomData is not BossRoomData bossRoomData)
                    throw new InvalidOperationException(
                        $"{roomNode.RoomPrefab.name} must contain BossRoomData.");
                ValidateBossRoom(roomNode.RoomPrefab, bossRoomData);
                return;
            case RoomType.Enemy:
            case RoomType.Exit:
                if (roomData == null)
                    throw new InvalidOperationException(
                        $"{roomNode.RoomPrefab.name} does not contain room data.");
                if (roomData is DefaultEnemiesRoomData enemiesRoomData)
                    ValidateKeyRoomSpawnPoint(roomNode.RoomPrefab.name, enemiesRoomData);
                if (roomNode.RoomPrefab.GetComponentInChildren<Features.Bosses.Scripts.BossSpawnPoint>(true) == null)
                    ValidateEnemySettings(roomNode.RoomPrefab.name, roomNode.EnemySettings);
                return;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static void ValidateRuntimeRoomData(LevelRoomNode roomNode)
    {
        switch (roomNode.Type, roomNode.Room.RoomData)
        {
            case (RoomType.Start, StartRoomData startRoomData):
                if (startRoomData.StartPoint == null)
                    throw new InvalidOperationException(
                        $"{roomNode.Room.name} does not have a start point.");
                return;
            case (RoomType.Enemy or RoomType.Exit, DefaultEnemiesRoomData enemiesRoomData):
                if (roomNode.Room.GetComponentInChildren<Features.Bosses.Scripts.BossSpawnPoint>(true) == null)
                    ValidateEnemySettings(roomNode.Room.name, enemiesRoomData.EnemySettings);
                ValidateKeyRoomSpawnPoint(roomNode.Room.name, enemiesRoomData);
                return;
            case (RoomType.Reward, RewardRoomData):
                return;
            case (RoomType.Shop, ShopRoomData):
                return;
            case (RoomType.Boss, BossRoomData bossRoomData):
                ValidateBossRoom(roomNode.Room, bossRoomData);
                return;
            default:
                throw new InvalidOperationException(
                    $"{roomNode.Room.name} data does not match its {roomNode.Type} room type.");
        }
    }

    private static void ValidateBossRoom(Room room, BossRoomData roomData)
    {
        var spawnPoint = room.GetComponentInChildren<Features.Bosses.Scripts.BossSpawnPoint>(true);
        if (spawnPoint == null || spawnPoint.BossPrefab == null)
            throw new InvalidOperationException(
                $"{room.name} must contain a BossSpawnPoint with a boss prefab.");

        ValidateKeyRoomSpawnPoint(room.name, roomData);
    }

    private static void ValidateEnemySettings(string roomName,
        EnemyRoomSettings enemySettings)
    {
        if (enemySettings == null)
            throw new InvalidOperationException(
                $"{roomName} does not contain enemy room settings.");

        if (!enemySettings.HasSpawnableEnemies)
            throw new InvalidOperationException(
                $"{roomName} enemy room settings do not contain spawnable enemies.");
    }

    private static void ValidateKeyRoomSpawnPoint(string roomName,
        DefaultEnemiesRoomData roomData)
    {
        if (roomData.CanSpawnKeyRoom && roomData.KeyRoomSpawnPoint == null)
        {
            throw new InvalidOperationException(
                $"{roomName} allows Key_Room spawning but does not have a spawn point.");
        }
    }

    private void ValidateKeyRoomConfiguration(bool useRuntimeRooms)
    {
        bool hasKeyRoomCandidate = _rooms.Any(roomNode =>
        {
            Room room = useRuntimeRooms ? roomNode?.Room : roomNode?.RoomPrefab;
            return room?.RoomData is DefaultEnemiesRoomData { CanSpawnKeyRoom: true };
        });

        if (!hasKeyRoomCandidate)
            return;

        if (_keyRoomPrefab == null)
            throw new InvalidOperationException(
                $"{name} contains Key_Room candidates but does not have a Key_Room prefab.");

        KeyRoomController controller = _keyRoomPrefab.GetComponent<KeyRoomController>();
        if (controller == null)
        {
            throw new InvalidOperationException(
                $"{_keyRoomPrefab.name} must contain KeyRoomController on its root.");
        }

        controller.ValidateConfiguration();
    }

    private static void ValidateRoomDoors(IEnumerable<Room> rooms)
    {
        foreach (Room room in rooms)
        {
            if (room.RoomData?.RoomDoors == null || room.RoomData.RoomDoors.Length == 0)
                throw new InvalidOperationException($"{room.name} does not have configured doors.");

            var directions = new HashSet<RoomDirection>();
            foreach (RoomDoor roomDoor in room.RoomData.RoomDoors)
            {
                if (roomDoor == null)
                    throw new InvalidOperationException($"{room.name} contains a missing door.");
                if (!roomDoor.HasConfiguredVisuals)
                    throw new InvalidOperationException(
                        $"{roomDoor.name} must contain assigned EnemyDoor and RewardDoor roots and two door leaves for each variant.");
                if (!directions.Add(roomDoor.Direction))
                    throw new InvalidOperationException(
                        $"{room.name} contains duplicate {roomDoor.Direction} doors.");
            }
        }
    }

    private void ValidateRequiredDoors(
        IReadOnlyDictionary<Vector2Int, Room> roomsByPosition, bool hasNextLevel)
    {
        if (!hasNextLevel)
            return;

        LevelRoomNode exitNode = GetExitRoomNode();
        Room exitRoom = roomsByPosition[exitNode.GridPosition];
        if (!HasDoor(exitRoom, exitNode.LevelExitDirection))
            throw new InvalidOperationException(
                $"{exitRoom.name} does not contain the " +
                $"{exitNode.LevelExitDirection} level-exit door.");
    }

    private static bool HasDoor(Room room, RoomDirection direction) =>
        FindDoor(room, direction) != null;

    public IReadOnlyCollection<RoomDirection> GetRoomDirections(LevelRoomNode roomNode)
    {
        if (roomNode == null || roomNode.RoomPrefab == null)
            throw new InvalidOperationException($"{name} contains a missing room prefab.");
        if (_rooms == null)
            throw new InvalidOperationException($"{name} does not contain room nodes.");

        var nodesByPosition = _rooms.Where(node => node != null)
            .ToDictionary(node => node.GridPosition);
        int rotation = GetMatchingRotation(roomNode.RoomPrefab,
            GetRequiredDirections(roomNode, nodesByPosition));
        return GetAvailableDirections(roomNode.RoomPrefab, authored: true)
            .Select(direction => direction.RotateClockwise(rotation))
            .Where(direction => IsConnectionAllowed(roomNode, direction, nodesByPosition))
            .ToArray();
    }

    private static HashSet<RoomDirection> GetRequiredDirections(
        LevelRoomNode roomNode, IReadOnlyDictionary<Vector2Int, LevelRoomNode> nodesByPosition)
    {
        var required = new HashSet<RoomDirection>();
        foreach (RoomDirection direction in CardinalDirections)
        {
            if (nodesByPosition.ContainsKey(roomNode.GridPosition + direction.ToGridOffset()) &&
                IsConnectionAllowed(roomNode, direction, nodesByPosition))
                required.Add(direction);
        }

        if (roomNode.Type is RoomType.Exit or RoomType.Boss)
        {
            if (!IsConnectionAllowed(roomNode, roomNode.LevelExitDirection, nodesByPosition))
                throw new InvalidOperationException("The level-exit direction cannot be blocked.");
            required.Add(roomNode.LevelExitDirection);
        }
        return required;
    }

    private static bool IsConnectionAllowed(LevelRoomNode roomNode,
        RoomDirection direction, IReadOnlyDictionary<Vector2Int, LevelRoomNode> nodesByPosition)
    {
        if (IsConnectionBlocked(roomNode, direction, nodesByPosition))
            return false;

        if (roomNode.Type == RoomType.Reward &&
            direction != GetRewardEntranceDirection(roomNode, nodesByPosition))
            return false;

        return !nodesByPosition.TryGetValue(roomNode.GridPosition + direction.ToGridOffset(),
                   out LevelRoomNode neighbour) ||
               neighbour.Type != RoomType.Reward ||
               direction.Opposite() == GetRewardEntranceDirection(neighbour, nodesByPosition);
    }

    private static RoomDirection GetRewardEntranceDirection(LevelRoomNode roomNode,
        IReadOnlyDictionary<Vector2Int, LevelRoomNode> nodesByPosition)
    {
        // Reward rooms are leaves of the level graph, even when several grid cells
        // touch them. The same choice is used by both sides of every connection.
        foreach (RoomDirection direction in CardinalDirections)
        {
            if (nodesByPosition.TryGetValue(roomNode.GridPosition + direction.ToGridOffset(),
                    out LevelRoomNode neighbour) &&
                neighbour.Type != RoomType.Reward &&
                !IsConnectionBlocked(roomNode, direction, nodesByPosition))
                return direction;
        }

        throw new InvalidOperationException(
            $"Reward room at {roomNode.GridPosition} must have an unblocked connection " +
            "to a non-reward room. Reward rooms cannot be used as passages.");
    }

    private static bool IsConnectionBlocked(LevelRoomNode roomNode,
        RoomDirection direction, IReadOnlyDictionary<Vector2Int, LevelRoomNode> nodesByPosition)
    {
        return (roomNode.BlockedConnections & direction.ToConnectionMask()) != RoomConnectionMask.None ||
               nodesByPosition.TryGetValue(roomNode.GridPosition + direction.ToGridOffset(),
                   out LevelRoomNode neighbour) &&
               (neighbour.BlockedConnections & direction.Opposite().ToConnectionMask()) !=
               RoomConnectionMask.None;
    }

    private static int GetMatchingRotation(Room room,
        IReadOnlyCollection<RoomDirection> requiredDirections)
    {
        if (TryMatchRotation(room, requiredDirections, out int rotation))
            return rotation;

        throw new InvalidOperationException(
            $"{room.name} cannot fit the required doors " +
            $"({string.Join(", ", requiredDirections)}) at any 90-degree rotation. " +
            "Use a prefab with matching entrances or move the room to a compatible grid cell.");
    }

    private static bool TryMatchRotation(Room room,
        IReadOnlyCollection<RoomDirection> requiredDirections, out int rotation)
    {
        HashSet<RoomDirection> authoredDirections =
            GetAvailableDirections(room, authored: true);
        for (int candidate = 0; candidate < 4; candidate++)
        {
            bool matches = true;
            foreach (RoomDirection required in requiredDirections)
            {
                if (authoredDirections.Contains(required.RotateClockwise(-candidate)))
                    continue;

                matches = false;
                break;
            }

            if (matches)
            {
                rotation = candidate;
                return true;
            }
        }

        rotation = 0;
        return false;
    }

    private static void ApplyRoomRotation(Room room, int rotation)
    {
        room.transform.localRotation = Quaternion.Euler(0f, rotation * 90f, 0f);
        foreach (RoomDoor door in room.GetComponentsInChildren<RoomDoor>(true))
            door.SetRoomRotation(rotation);
    }

    private static HashSet<RoomDirection> GetAvailableDirections(Room room,
        bool authored = false)
    {
        RoomDoor[] configuredDoors = room.RoomData?.RoomDoors;
        if (configuredDoors == null || configuredDoors.Length == 0)
            throw new InvalidOperationException(
                $"{room.name} does not have active doors configured in RoomData.");

        var result = new HashSet<RoomDirection>();
        foreach (RoomDoor door in configuredDoors)
        {
            if (door == null)
                throw new InvalidOperationException(
                    $"{room.name} contains a missing active door in RoomData.");
            if (!door.HasConfiguredVisuals)
                throw new InvalidOperationException(
                    $"{door.name} must contain assigned EnemyDoor and RewardDoor roots and two door leaves for each variant.");
            RoomDirection direction = authored ? door.AuthoredDirection : door.Direction;
            if (!result.Add(direction))
                throw new InvalidOperationException(
                    $"{room.name} contains duplicate {direction} doors.");
        }

        return result;
    }

    private void ConnectAdjacentRooms(IReadOnlyDictionary<Vector2Int, Room> roomsByPosition)
    {
        var nodesByPosition = _rooms.ToDictionary(node => node.GridPosition);
        foreach (KeyValuePair<Vector2Int, Room> roomEntry in roomsByPosition)
        {
            Room currentRoom = roomEntry.Value;
            foreach (RoomDoor currentDoor in currentRoom.RoomData.RoomDoors)
            {
                Vector2Int neighbourPosition =
                    roomEntry.Key + currentDoor.Direction.ToGridOffset();

                if (!roomsByPosition.TryGetValue(neighbourPosition, out Room neighbourRoom))
                    continue;
                if (!IsConnectionAllowed(nodesByPosition[roomEntry.Key],
                        currentDoor.Direction, nodesByPosition))
                    continue;

                RoomDoor neighbourDoor = FindDoor(neighbourRoom,
                    currentDoor.Direction.Opposite());
                if (neighbourDoor == null)
                    continue;

                currentDoor.Configure(neighbourRoom, neighbourDoor);
            }
        }
    }

    private void ConfigureStartPointRotation()
    {
        if (StartRoom.RoomData is not StartRoomData startRoomData ||
            startRoomData.StartPoint == null || startRoomData.RoomDoors == null)
            return;

        Transform startPoint = startRoomData.StartPoint;
        foreach (RoomDoor door in startRoomData.RoomDoors)
        {
            if (door == null || !door.HasRoomDestination)
                continue;

            Vector3 direction = door.transform.position - startPoint.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
                continue;

            startPoint.rotation = Quaternion.LookRotation(direction, Vector3.up);
            return;
        }
    }

    private void ConfigureLevelExit(bool hasNextLevel)
    {
        if (!hasNextLevel)
            return;

        LevelRoomNode roomNode = GetExitRoomNode();
        RoomDoor exitDoor = GetRequiredDoor(roomNode.Room, roomNode.LevelExitDirection);
        if (exitDoor.HasRoomDestination)
            throw new InvalidOperationException(
                $"{roomNode.Room.name} level exit direction is occupied by another room.");

        exitDoor.ConfigureLevelExit();
    }

    public void MarkKeyRewardDropped() => HasDroppedKeyReward = true;

    public bool RegisterRewardBagForGuaranteedKey()
    {
        _spawnedRewardBags++;
        if (_spawnedRewardBags < 3 || HasDroppedKeyReward)
            return false;

        return _spawnedRewardBags >= _guaranteedKeyRewardBagNumber ||
               CanReachKeyRoomBeforeNextReward();
    }

    private bool CanReachKeyRoomBeforeNextReward()
    {
        int nonStartRoomCount = _rooms.Count(roomNode =>
            roomNode != null && roomNode.Type != RoomType.Start);
        if (nonStartRoomCount == 0)
            return false;

        var nodes = _rooms.ToDictionary(node => node.GridPosition);
        var reached = new HashSet<Vector2Int> { StartRoomGridPosition };
        var pending = new Queue<Vector2Int>();
        pending.Enqueue(StartRoomGridPosition);

        int reachableUnvisitedNonCombatRooms = 0;
        bool hasReachableKeyRoomCandidate = false;
        while (pending.Count > 0)
        {
            Vector2Int position = pending.Dequeue();
            foreach (RoomDirection direction in CardinalDirections)
            {
                Vector2Int neighbourPosition = position + direction.ToGridOffset();
                if (!nodes.TryGetValue(neighbourPosition, out LevelRoomNode neighbour))
                    continue;
                if (!IsConnectionAllowed(nodes[position], direction, nodes) ||
                    !reached.Add(neighbourPosition))
                    continue;

                RoomData neighbourData = neighbour.Room.RoomData;
                bool isVisited = _keyRoomVisitedRooms.Contains(neighbourData);
                if (!isVisited && neighbourData is DefaultEnemiesRoomData enemiesRoomData)
                {
                    hasReachableKeyRoomCandidate |= enemiesRoomData.CanSpawnKeyRoom;
                    continue;
                }

                if (!isVisited && neighbour.Type != RoomType.Start)
                    reachableUnvisitedNonCombatRooms++;

                pending.Enqueue(neighbourPosition);
            }
        }

        float nextCombatProgress =
            (float)(_visitedNonStartRooms + reachableUnvisitedNonCombatRooms + 1) /
            nonStartRoomCount;
        return hasReachableKeyRoomCandidate &&
               nextCombatProgress >= Mathf.Clamp01(_keyRoomStartProgressPercent / 100f);
    }

    public bool TrySpawnKeyRoom(Room room, RoomDoor entryDoor)
    {
        if (!_isInitialized || _container == null)
            throw new InvalidOperationException($"{name} is not initialized.");
        if (room == null)
            throw new ArgumentNullException(nameof(room));
        if (entryDoor == null)
            throw new ArgumentNullException(nameof(entryDoor));
        if (!IsRoomOwnedByLevel(room))
            throw new InvalidOperationException(
                $"{room.name} does not belong to {name}.");

        RoomData roomData = room.RoomData ??
                            throw new InvalidOperationException(
                                $"{room.name} does not contain room data.");
        if (!_keyRoomVisitedRooms.Add(roomData))
            return false;
        if (roomData is StartRoomData)
            return false;

        _visitedNonStartRooms++;

        if (_roomsUntilNextKeyRoomChance > 0)
        {
            _roomsUntilNextKeyRoomChance--;
            return false;
        }

        int nonStartRoomCount = _rooms.Count(roomNode =>
            roomNode != null && roomNode.Type != RoomType.Start);
        if (nonStartRoomCount == 0)
            return false;

        float levelProgress = (float)_visitedNonStartRooms / nonStartRoomCount;
        if (levelProgress < Mathf.Clamp01(_keyRoomStartProgressPercent / 100f))
            return false;

        if (roomData is not DefaultEnemiesRoomData enemiesRoomData ||
            !enemiesRoomData.CanSpawnKeyRoom)
        {
            return false;
        }

        ValidateKeyRoomSpawnPoint(room.name, enemiesRoomData);

        bool mustGuaranteeFirstSpawn = _spawnedKeyRooms == 0 &&
                                       !HasUnvisitedKeyRoomCandidate();
        if (!mustGuaranteeFirstSpawn && !PassedKeyRoomSpawnChance())
            return false;

        SpawnKeyRoom(room, enemiesRoomData.KeyRoomSpawnPoint, entryDoor);
        _spawnedKeyRooms++;
        _roomsUntilNextKeyRoomChance = Mathf.Max(0, _roomsBetweenKeyRoomSpawns);
        return true;
    }

    private bool PassedKeyRoomSpawnChance()
    {
        float chance = Mathf.Clamp01(_keyRoomSpawnChancePercent / 100f);
        return chance >= 1f || chance > 0f && UnityEngine.Random.value < chance;
    }

    private bool HasUnvisitedKeyRoomCandidate() =>
        _rooms.Any(roomNode =>
        {
            if (roomNode?.Room?.RoomData is not DefaultEnemiesRoomData roomData)
                return false;

            return roomData.CanSpawnKeyRoom &&
                   !_keyRoomVisitedRooms.Contains(roomData);
        });

    private void SpawnKeyRoom(Room room, Transform spawnPoint, RoomDoor entryDoor)
    {
        GameObject keyRoom = _container.InstantiatePrefab(_keyRoomPrefab,
            spawnPoint.position, spawnPoint.rotation, room.transform);

        KeyRoomController controller = keyRoom.GetComponent<KeyRoomController>();
        if (controller == null)
        {
            throw new InvalidOperationException(
                $"{keyRoom.name} must contain KeyRoomController on its root.");
        }

        controller.Initialize(room, entryDoor.transform.forward);
        ConfigureKeyRoomEnemyExclusion(keyRoom);
    }

    private static void ConfigureKeyRoomEnemyExclusion(GameObject keyRoom)
    {
        int wallLayer = LayerMask.NameToLayer(WallLayerName);
        if (wallLayer < 0)
            throw new InvalidOperationException($"Layer {WallLayerName} does not exist.");

        int notWalkableArea = NavMesh.GetAreaFromName(NotWalkableAreaName);
        if (notWalkableArea < 0)
        {
            throw new InvalidOperationException(
                $"NavMesh area {NotWalkableAreaName} does not exist.");
        }

        Collider[] colliders = keyRoom.GetComponentsInChildren<Collider>(true)
            .Where(collider => collider.enabled && !collider.isTrigger &&
                               collider.gameObject.activeInHierarchy)
            .ToArray();
        if (colliders.Length == 0)
            throw new InvalidOperationException(
                $"{keyRoom.name} does not contain colliders for enemy exclusion.");

        foreach (Collider collider in colliders)
            collider.gameObject.layer = wallLayer;

        Physics.SyncTransforms();
        Bounds localBounds = GetLocalBounds(keyRoom.transform, colliders);

        var exclusionObject = new GameObject("EnemyNavMeshExclusion")
        {
            layer = wallLayer
        };
        exclusionObject.transform.SetParent(keyRoom.transform, false);

        NavMeshModifierVolume modifier =
            exclusionObject.AddComponent<NavMeshModifierVolume>();
        modifier.center = localBounds.center;
        modifier.size = localBounds.size;
        modifier.area = notWalkableArea;
    }

    private static Bounds GetLocalBounds(Transform root,
        IReadOnlyList<Collider> colliders)
    {
        Bounds localBounds = default;
        bool hasPoint = false;

        foreach (Collider collider in colliders)
        {
            Bounds worldBounds = collider.bounds;
            for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
            for (int z = 0; z < 2; z++)
            {
                Vector3 worldPoint = new(
                    x == 0 ? worldBounds.min.x : worldBounds.max.x,
                    y == 0 ? worldBounds.min.y : worldBounds.max.y,
                    z == 0 ? worldBounds.min.z : worldBounds.max.z);
                Vector3 localPoint = root.InverseTransformPoint(worldPoint);

                if (!hasPoint)
                {
                    localBounds = new Bounds(localPoint, Vector3.zero);
                    hasPoint = true;
                }
                else
                {
                    localBounds.Encapsulate(localPoint);
                }
            }
        }

        return localBounds;
    }

    private void ResetKeyRoomSpawnState()
    {
        _keyRoomVisitedRooms.Clear();
        _visitedNonStartRooms = 0;
        _spawnedKeyRooms = 0;
        _roomsUntilNextKeyRoomChance = 0;

        if (StartRoom?.RoomData != null)
            _keyRoomVisitedRooms.Add(StartRoom.RoomData);
    }

    private void ResetKeyRewardState()
    {
        HasDroppedKeyReward = false;
        _spawnedRewardBags = 0;

        int nonStartRoomCount = _rooms.Count(roomNode =>
            roomNode != null && roomNode.Type != RoomType.Start);
        int combatRoomCount = _rooms.Count(roomNode =>
            roomNode?.Room?.RoomData is DefaultEnemiesRoomData);
        int lastGuaranteedBagNumber = Mathf.Max(3,
            Mathf.CeilToInt(nonStartRoomCount *
                            Mathf.Clamp01(_keyRoomStartProgressPercent / 100f)) - 1);
        lastGuaranteedBagNumber = Mathf.Min(lastGuaranteedBagNumber,
            Mathf.Max(3, combatRoomCount));
        _guaranteedKeyRewardBagNumber = UnityEngine.Random.Range(3,
            lastGuaranteedBagNumber + 1);
    }

    public bool IsExitRoom(RoomData roomData) =>
        roomData != null && _rooms.Any(roomNode =>
            (roomNode.Type is RoomType.Exit or RoomType.Boss) && roomNode.Room != null &&
            ReferenceEquals(roomNode.Room.RoomData, roomData));

    public int GetEnemyRoomIndex(RoomData roomData)
    {
        if (roomData == null)
            throw new ArgumentNullException(nameof(roomData));

        for (int i = 0; i < _rooms.Length; i++)
        {
            LevelRoomNode roomNode = _rooms[i];
            if (roomNode == null || roomNode.Type is not (RoomType.Enemy or RoomType.Exit or RoomType.Boss))
                continue;

            if (ReferenceEquals(roomNode.Room?.RoomData, roomData))
            {
                _combatDepths ??= BuildCombatDepths();
                return Mathf.Max(0, _combatDepths[roomNode.GridPosition] - 1);
            }
        }

        throw new InvalidOperationException($"{name} does not contain the provided enemy room data.");
    }

    public int GetCombatRoomsToExit()
    {
        _combatDepths ??= BuildCombatDepths();
        return Mathf.Max(1, _combatDepths[GetExitRoomNode().GridPosition]);
    }

    private Dictionary<Vector2Int, int> BuildCombatDepths()
    {
        var nodes = _rooms.ToDictionary(node => node.GridPosition);
        var depths = new Dictionary<Vector2Int, int> { [StartRoomGridPosition] = 0 };
        var pending = new Queue<Vector2Int>();
        pending.Enqueue(StartRoomGridPosition);

        // Rotations and prefab variants must fit every allowed grid connection.
        // Count the intended routes before instantiation, without depending on
        // the unrotated prefab doors. Optional rooms do not raise other branches' difficulty.
        while (pending.Count > 0)
        {
            Vector2Int position = pending.Dequeue();
            foreach (RoomDirection direction in CardinalDirections)
            {
                Vector2Int neighbourPosition = position + direction.ToGridOffset();
                if (!nodes.TryGetValue(neighbourPosition, out LevelRoomNode neighbour))
                    continue;
                if (!IsConnectionAllowed(nodes[position], direction, nodes))
                    continue;

                int depth = depths[position] +
                            (neighbour.Type is RoomType.Enemy or RoomType.Exit or RoomType.Boss ? 1 : 0);
                if (depths.TryGetValue(neighbourPosition, out int previousDepth) && previousDepth <= depth)
                    continue;

                depths[neighbourPosition] = depth;
                pending.Enqueue(neighbourPosition);
            }
        }

        return depths;
    }

    private void ResetRoomProgress()
    {
        foreach (LevelRoomNode roomNode in _rooms)
        {
            switch (roomNode?.Room?.RoomData)
            {
                case DefaultEnemiesRoomData enemiesRoomData:
                    enemiesRoomData.ResetProgress();
                    break;
                case RewardRoomData rewardRoomData:
                    rewardRoomData.ResetProgress();
                    break;
            }
        }
    }

    private static void ResetDoors(IEnumerable<Room> rooms)
    {
        foreach (Room room in rooms)
        {
            foreach (RoomDoor door in room.GetComponentsInChildren<RoomDoor>(true))
                door.ClearDestination();
        }
    }

    private void ValidateConnectivity(
        IReadOnlyDictionary<Vector2Int, Room> roomsByPosition)
    {
        var nodesByPosition = _rooms.ToDictionary(node => node.GridPosition);
        ValidateConnectivity(roomsByPosition.ToDictionary(
            entry => entry.Key,
            entry => (IReadOnlyCollection<RoomDirection>)GetAvailableDirections(entry.Value)
                .Where(direction => IsConnectionAllowed(nodesByPosition[entry.Key],
                    direction, nodesByPosition)).ToArray()));
    }

    private void ValidateConnectivity(
        IReadOnlyDictionary<Vector2Int, IReadOnlyCollection<RoomDirection>> directionsByPosition)
    {
        var visited = new HashSet<Vector2Int>();
        var pending = new Queue<Vector2Int>();
        pending.Enqueue(StartRoomGridPosition);

        while (pending.Count > 0)
        {
            Vector2Int position = pending.Dequeue();
            if (!visited.Add(position))
                continue;

            foreach (RoomDirection direction in directionsByPosition[position])
            {
                Vector2Int neighbourPosition =
                    position + direction.ToGridOffset();
                if (!directionsByPosition.TryGetValue(neighbourPosition,
                        out IReadOnlyCollection<RoomDirection> neighbourDirections) ||
                    !neighbourDirections.Contains(direction.Opposite()) ||
                    visited.Contains(neighbourPosition))
                    continue;

                pending.Enqueue(neighbourPosition);
            }
        }

        if (visited.Count != directionsByPosition.Count)
            throw new InvalidOperationException(
                $"{name} contains rooms that cannot be reached from the start room.");
    }

    private static RoomDoor FindDoor(Room room, RoomDirection direction) =>
        room.RoomData.RoomDoors.FirstOrDefault(door =>
            door != null && door.Direction == direction);

    private static RoomDoor GetRequiredDoor(Room room, RoomDirection direction)
    {
        RoomDoor roomDoor = FindDoor(room, direction);
        if (roomDoor != null)
            return roomDoor;

        throw new InvalidOperationException($"{room.name} does not contain a {direction} door.");
    }

    private bool IsRoomOwnedByLevel(Room room) =>
        room != null && room.transform.IsChildOf(transform);

    private void PositionEmbeddedRoom(LevelRoomNode roomNode)
    {
        Room room = roomNode.Room;
        if (!IsRoomOwnedByLevel(room))
            return;

        if (room.transform.parent != transform)
            room.transform.SetParent(transform, false);
        room.transform.SetLocalPositionAndRotation(
            ToWorldPosition(roomNode.GridPosition), Quaternion.identity);
        var nodesByPosition = _rooms.Where(node => node != null)
            .ToDictionary(node => node.GridPosition);
        ApplyRoomRotation(room,
            GetMatchingRotation(room, GetRequiredDirections(roomNode, nodesByPosition)));
    }

    private void OnDrawGizmos()
    {
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Color previousColor = Gizmos.color;
        Gizmos.matrix = transform.localToWorldMatrix;

        if (_rooms != null)
        {
            foreach (LevelRoomNode roomNode in _rooms)
            {
                if (roomNode == null)
                    continue;

                Color color = roomNode.Type switch
                {
                    RoomType.Start => Color.cyan,
                    RoomType.Exit => Color.green,
                    RoomType.Reward => Color.yellow,
                    RoomType.Shop => Color.magenta,
                    RoomType.Boss => new Color(0.85f, 0.2f, 0.2f, 1f),
                    _ => Color.white
                };
                DrawRoomGizmo(roomNode.GridPosition, color);
            }
        }

        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }

    private static void DrawRoomGizmo(Vector2Int gridPosition, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawWireCube(ToWorldPosition(gridPosition),
            new Vector3(RoomWorldSize, 1f, RoomWorldSize));
    }

    private static Vector3 ToWorldPosition(Vector2Int gridPosition) =>
        new(gridPosition.x * RoomWorldSize, 0f, gridPosition.y * RoomWorldSize);

    private LevelRoomNode GetExitRoomNode()
    {
        if (_rooms == null)
            throw new InvalidOperationException($"{name} does not contain room nodes.");

        LevelRoomNode[] matches = _rooms
            .Where(roomNode => roomNode != null &&
                (roomNode.Type is RoomType.Exit or RoomType.Boss))
            .ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException(
                $"{name} must contain exactly one final room (Exit or Boss).");

        return matches[0];
    }

    private LevelRoomNode GetRoomNode(RoomType roomType)
    {
        if (_rooms == null)
            throw new InvalidOperationException($"{name} does not contain room nodes.");

        LevelRoomNode[] matches = _rooms
            .Where(roomNode => roomNode != null && roomNode.Type == roomType)
            .ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException(
                $"{name} must contain exactly one {roomType.ToString().ToLowerInvariant()} room.");

        return matches[0];
    }

}

public enum RoomType
{
    Start,
    Exit,
    Enemy,
    Reward,
    Shop,
    Boss = 5
}

[Serializable]
public sealed class LevelRoomNode
{
    [FormerlySerializedAs("<Room>k__BackingField")]
    [SerializeField] private Room _roomPrefab;
    [NonSerialized] private Room _room;

    public Room RoomPrefab => _roomPrefab;
    public Room Room => _room;
    [field: SerializeField] public Vector2Int GridPosition { get; private set; }
    [field: SerializeField]
    [field: Tooltip("Grid directions without a passage, even when another room is adjacent. Not affected by room rotation.")]
    public RoomConnectionMask BlockedConnections { get; private set; }
    [field: SerializeField] public RoomType Type { get; private set; } = RoomType.Enemy;
    [field: SerializeField]
    [field: Tooltip("Used by combat rooms (Enemy and Exit).")]
    public EnemyRoomSettings EnemySettings { get; private set; } = new();
    [field: SerializeField]
    [field: Tooltip("Used when Type is Exit or Boss. Opens after the room is cleared.")]
    public RoomDirection LevelExitDirection { get; private set; }

    public LevelRoomNode(Room roomPrefab, Vector2Int gridPosition, RoomType type,
        RoomDirection levelExitDirection = default,
        EnemyRoomSettings enemySettings = null)
    {
        _roomPrefab = roomPrefab;
        GridPosition = gridPosition;
        Type = type;
        LevelExitDirection = levelExitDirection;
        EnemySettings = enemySettings ?? new EnemyRoomSettings();
    }

    public void Bind(Room room) =>
        _room = room != null
            ? room
            : throw new ArgumentNullException(nameof(room));
}
