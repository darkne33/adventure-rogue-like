using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

public class LevelView : MonoBehaviour
{
    public const float RoomWorldSize = 160f;

    [SerializeField] private LevelRoomNode[] _rooms;

    public Room StartRoomPrefab => GetRoomNode(RoomType.Start).RoomPrefab;
    public Room StartRoom => GetRoomNode(RoomType.Start).Room;
    public Vector2Int StartRoomGridPosition => GetRoomNode(RoomType.Start).GridPosition;
    public IReadOnlyList<LevelRoomNode> Rooms => _rooms;

    private bool _isInitialized;

    public void Configure(LevelRoomNode[] rooms)
    {
        _rooms = rooms;
        _isInitialized = false;
    }

    public void Initialize(DiContainer container, bool hasNextLevel)
    {
        if (_isInitialized)
            return;

        if (container == null)
            throw new ArgumentNullException(nameof(container));

        MaterializeRooms(container);
        ResolveAuthoredDoors();
        ResetRoomProgress();

        Dictionary<Vector2Int, Room> roomsByPosition = ValidateAndBuildRoomMap();
        ValidateRoomDoors(roomsByPosition.Values);
        ValidateRequiredDoors(roomsByPosition, hasNextLevel);
        ValidateConnectivity(roomsByPosition);
        ResetDoors(roomsByPosition.Values);
        ConnectAdjacentRooms(roomsByPosition);
        ConfigureLevelExit(hasNextLevel);

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
            PositionEmbeddedRoom(roomNode.Room, roomNode.GridPosition);
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
            if (roomNode.Type == RoomType.Exit)
                exitCount++;
        }

        ValidateTopology(sourcesByPosition, startCount, exitCount);
        ValidateAuthoringDoors(sourcesByPosition);
        ValidateConnectivity(sourcesByPosition);

        LevelRoomNode exitNode = GetRoomNode(RoomType.Exit);
        if (!GetAvailableDirections(exitNode.RoomPrefab)
                .Contains(exitNode.LevelExitDirection))
        {
            throw new InvalidOperationException(
                $"{exitNode.RoomPrefab.name} does not contain the " +
                $"{exitNode.LevelExitDirection} level-exit door.");
        }
    }

    private void MaterializeRooms(DiContainer container)
    {
        if (_rooms == null || _rooms.Length == 0)
            throw new InvalidOperationException($"{name} does not contain room nodes.");

        foreach (LevelRoomNode roomNode in _rooms)
        {
            if (roomNode == null)
                throw new InvalidOperationException($"{name} contains a missing room node.");
            if (roomNode.RoomPrefab == null)
                throw new InvalidOperationException($"{name} contains a missing room prefab.");

            Room room = MaterializeRoom(container, roomNode.RoomPrefab,
                roomNode.Room, roomNode.GridPosition, "room");

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

    private void ResolveAuthoredDoors()
    {
        foreach (Room room in _rooms.Select(roomNode => roomNode.Room))
        {
            if (room?.RoomData == null)
                throw new InvalidOperationException($"{room?.name ?? name} does not have room data.");

            RoomDoor[] authoredDoors = room.GetComponentsInChildren<RoomDoor>(true)
                .OrderBy(door => door.Direction)
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
                if (!configuredDirections.Add(configuredDoor.Direction))
                    throw new InvalidOperationException(
                        $"{room.name} contains duplicate {configuredDoor.Direction} active doors.");

                RoomDoor[] matches = authoredDoors
                    .Where(door => door.Direction == configuredDoor.Direction)
                    .ToArray();
                if (matches.Length != 1)
                    throw new InvalidOperationException(
                        $"{room.name} must contain exactly one authored {configuredDoor.Direction} door.");

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
            if (roomNode.Type == RoomType.Exit)
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
                $"{name} must contain exactly one exit room.");

        LevelRoomNode exitNode = GetRoomNode(RoomType.Exit);
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
            case RoomType.Enemy:
            case RoomType.Exit:
                if (roomData == null)
                    throw new InvalidOperationException(
                        $"{roomNode.RoomPrefab.name} does not contain room data.");
                ValidateEnemySettings(roomNode.RoomPrefab.name,
                    roomNode.EnemySettings);
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
                ValidateEnemySettings(roomNode.Room.name,
                    enemiesRoomData.EnemySettings);
                return;
            case (RoomType.Reward, RewardRoomData):
                return;
            case (RoomType.Shop, ShopRoomData):
                return;
            default:
                throw new InvalidOperationException(
                    $"{roomNode.Room.name} data does not match its {roomNode.Type} room type.");
        }
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

        LevelRoomNode exitNode = GetRoomNode(RoomType.Exit);
        Room exitRoom = roomsByPosition[exitNode.GridPosition];
        if (!HasDoor(exitRoom, exitNode.LevelExitDirection))
            throw new InvalidOperationException(
                $"{exitRoom.name} does not contain the " +
                $"{exitNode.LevelExitDirection} level-exit door.");
    }

    private static bool HasDoor(Room room, RoomDirection direction) =>
        FindDoor(room, direction) != null;

    private void ValidateAuthoringDoors(
        IReadOnlyDictionary<Vector2Int, Room> sourcesByPosition)
    {
        foreach (Room room in sourcesByPosition.Values)
            GetAvailableDirections(room);
    }

    private static HashSet<RoomDirection> GetAvailableDirections(Room room)
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
            if (!result.Add(door.Direction))
                throw new InvalidOperationException(
                    $"{room.name} contains duplicate {door.Direction} doors.");
        }

        return result;
    }

    private void ConnectAdjacentRooms(IReadOnlyDictionary<Vector2Int, Room> roomsByPosition)
    {
        foreach (KeyValuePair<Vector2Int, Room> roomEntry in roomsByPosition)
        {
            Room currentRoom = roomEntry.Value;
            foreach (RoomDoor currentDoor in currentRoom.RoomData.RoomDoors)
            {
                Vector2Int neighbourPosition =
                    roomEntry.Key + currentDoor.Direction.ToGridOffset();

                if (!roomsByPosition.TryGetValue(neighbourPosition, out Room neighbourRoom))
                    continue;

                RoomDoor neighbourDoor = FindDoor(neighbourRoom,
                    currentDoor.Direction.Opposite());
                if (neighbourDoor == null)
                    continue;

                currentDoor.Configure(neighbourRoom, neighbourDoor);
            }
        }
    }

    private void ConfigureLevelExit(bool hasNextLevel)
    {
        if (!hasNextLevel)
            return;

        LevelRoomNode roomNode = GetRoomNode(RoomType.Exit);
        RoomDoor exitDoor = GetRequiredDoor(roomNode.Room, roomNode.LevelExitDirection);
        if (exitDoor.HasRoomDestination)
            throw new InvalidOperationException(
                $"{roomNode.Room.name} level exit direction is occupied by another room.");

        exitDoor.ConfigureLevelExit();
    }

    public bool IsExitRoom(RoomData roomData) =>
        roomData != null && _rooms.Any(roomNode =>
            roomNode.Type == RoomType.Exit && roomNode.Room != null &&
            ReferenceEquals(roomNode.Room.RoomData, roomData));

    public int GetEnemyRoomIndex(RoomData roomData)
    {
        if (roomData == null)
            throw new ArgumentNullException(nameof(roomData));

        int nonStartRoomIndex = 0;
        for (int i = 0; i < _rooms.Length; i++)
        {
            LevelRoomNode roomNode = _rooms[i];
            if (roomNode == null || roomNode.Type == RoomType.Start)
                continue;

            if (ReferenceEquals(roomNode.Room?.RoomData, roomData))
                return nonStartRoomIndex;

            nonStartRoomIndex++;
        }

        throw new InvalidOperationException($"{name} does not contain the provided enemy room data.");
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
        var visited = new HashSet<Vector2Int>();
        var pending = new Queue<Vector2Int>();
        pending.Enqueue(StartRoomGridPosition);

        while (pending.Count > 0)
        {
            Vector2Int position = pending.Dequeue();
            if (!visited.Add(position))
                continue;

            Room room = roomsByPosition[position];
            foreach (RoomDoor roomDoor in room.RoomData.RoomDoors)
            {
                Vector2Int neighbourPosition =
                    position + roomDoor.Direction.ToGridOffset();
                if (!roomsByPosition.TryGetValue(neighbourPosition,
                        out Room neighbourRoom) ||
                    !HasDoor(neighbourRoom, roomDoor.Direction.Opposite()) ||
                    visited.Contains(neighbourPosition))
                    continue;

                pending.Enqueue(neighbourPosition);
            }
        }

        if (visited.Count != roomsByPosition.Count)
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

    private void PositionEmbeddedRoom(Room room, Vector2Int gridPosition)
    {
        if (!IsRoomOwnedByLevel(room))
            return;

        if (room.transform.parent != transform)
            room.transform.SetParent(transform, false);
        room.transform.SetLocalPositionAndRotation(
            ToWorldPosition(gridPosition), Quaternion.identity);
    }

    private void OnDrawGizmosSelected()
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
    Shop
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
    [field: SerializeField] public RoomType Type { get; private set; } = RoomType.Enemy;
    [field: SerializeField]
    [field: Tooltip("Used by combat rooms (Enemy and Exit).")]
    public EnemyRoomSettings EnemySettings { get; private set; } = new();
    [field: SerializeField]
    [field: Tooltip("Used only when Type is Exit.")]
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
