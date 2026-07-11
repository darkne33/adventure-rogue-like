using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

public class LevelView : MonoBehaviour
{
    public const float RoomWorldSize = 160f;

    [FormerlySerializedAs("<StartRoom>k__BackingField")]
    [SerializeField] private Room _startRoomPrefab;
    [NonSerialized] private Room _startRoom;

    [SerializeField] private Vector2Int _startRoomGridPosition;
    [SerializeField] private LevelRoomNode[] _rooms;

    public Room StartRoomPrefab => _startRoomPrefab;
    public Room StartRoom => _startRoom;
    public Vector2Int StartRoomGridPosition => _startRoomGridPosition;
    public IReadOnlyList<LevelRoomNode> Rooms => _rooms;

    private bool _isInitialized;

    public void Configure(Room startRoomPrefab, Vector2Int startRoomGridPosition,
        LevelRoomNode[] rooms)
    {
        _startRoomPrefab = startRoomPrefab;
        _startRoomGridPosition = startRoomGridPosition;
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
        ResetDoors(roomsByPosition.Values);
        ConnectAdjacentRooms(roomsByPosition);
        ConfigureLevelExit(hasNextLevel);

        _isInitialized = true;
    }

    public void ResolveRoomReferences()
    {
        if (StartRoomPrefab == null)
            throw new InvalidOperationException($"{name} does not have a start room prefab.");
        if (_rooms == null || _rooms.Length == 0)
            throw new InvalidOperationException($"{name} does not contain room nodes.");

        _startRoom = StartRoomPrefab;
        PositionEmbeddedRoom(StartRoom, _startRoomGridPosition);

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
        if (StartRoomPrefab == null)
            throw new InvalidOperationException($"{name} does not have a start room prefab.");
        if (StartRoomPrefab.RoomData is not StartRoomData startRoomData)
            throw new InvalidOperationException(
                $"{StartRoomPrefab.name} must contain StartRoomData.");
        if (startRoomData.StartPoint == null)
            throw new InvalidOperationException(
                $"{StartRoomPrefab.name} does not have a start point.");
        if (_rooms == null || _rooms.Length == 0)
            throw new InvalidOperationException($"{name} does not contain room nodes.");

        var sourcesByPosition = new Dictionary<Vector2Int, Room>
        {
            { _startRoomGridPosition, StartRoomPrefab }
        };
        int exitCount = 0;

        foreach (LevelRoomNode roomNode in _rooms)
        {
            if (roomNode == null || roomNode.RoomPrefab == null)
                throw new InvalidOperationException($"{name} contains a missing room prefab.");

            ValidateSourceRoomData(roomNode);

            if (!sourcesByPosition.TryAdd(roomNode.GridPosition, roomNode.RoomPrefab))
                throw new InvalidOperationException(
                    $"{name} contains more than one room at {roomNode.GridPosition}.");

            if (roomNode.IsLevelExit)
                exitCount++;
        }

        ValidateExitTopology(sourcesByPosition, exitCount);
        ValidateConnectivity(sourcesByPosition.Keys);
        ValidateAuthoringDoors(sourcesByPosition);

        LevelRoomNode exitNode = _rooms.Single(roomNode => roomNode.IsLevelExit);
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
        if (StartRoomPrefab == null)
            throw new InvalidOperationException($"{name} does not have a start room prefab.");
        if (_rooms == null || _rooms.Length == 0)
            throw new InvalidOperationException($"{name} does not contain room nodes.");

        _startRoom = MaterializeRoom(container, StartRoomPrefab, StartRoom,
            _startRoomGridPosition, "start room");

        foreach (LevelRoomNode roomNode in _rooms)
        {
            if (roomNode == null)
                throw new InvalidOperationException($"{name} contains a missing room node.");
            if (roomNode.RoomPrefab == null)
                throw new InvalidOperationException($"{name} contains a missing room prefab.");

            DefaultRoom room = MaterializeRoom(container, roomNode.RoomPrefab,
                roomNode.Room, roomNode.GridPosition, "room");

            if (roomNode.IsRewardRoom && room.RoomData is not RewardRoomData)
            {
                RoomDoor[] authoredDoors = room.RoomData?.RoomDoors;
                room.SetRoomData(new RewardRoomData
                {
                    RoomDoors = authoredDoors
                });
            }

            roomNode.Bind(room);
        }
    }

    private void ResolveAuthoredDoors()
    {
        var rooms = new List<Room>(_rooms.Length + 1)
        {
            StartRoom
        };
        rooms.AddRange(_rooms.Select(roomNode => (Room)roomNode.Room));

        foreach (Room room in rooms)
        {
            if (room?.RoomData == null)
                throw new InvalidOperationException($"{room?.name ?? name} does not have room data.");

            RoomDoor[] authoredDoors = room.GetComponentsInChildren<RoomDoor>(true)
                .OrderBy(door => door.Direction)
                .ToArray();
            if (authoredDoors.Length == 0)
                throw new InvalidOperationException(
                    $"{room.name} does not contain authored RoomDoor objects.");

            room.RoomData.RoomDoors = authoredDoors;
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
        if (StartRoom == null)
            throw new InvalidOperationException($"{name} does not have a start room.");
        if (StartRoom.RoomData is not StartRoomData startRoomData)
            throw new InvalidOperationException($"{StartRoom.name} must contain StartRoomData.");
        if (startRoomData.StartPoint == null)
            throw new InvalidOperationException($"{StartRoom.name} does not have a start point.");

        var roomsByPosition = new Dictionary<Vector2Int, Room>
        {
            { _startRoomGridPosition, StartRoom }
        };
        var roomInstances = new HashSet<Room>
        {
            StartRoom
        };

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

            if (roomNode.IsLevelExit)
                exitCount++;
        }

        ValidateExitTopology(roomsByPosition, exitCount);
        ValidateConnectivity(roomsByPosition.Keys);
        return roomsByPosition;
    }

    private void ValidateExitTopology<T>(IReadOnlyDictionary<Vector2Int, T> roomsByPosition,
        int exitCount)
    {
        if (exitCount != 1)
            throw new InvalidOperationException(
                $"{name} must contain exactly one exit room.");

        LevelRoomNode exitNode = _rooms.Single(roomNode => roomNode.IsLevelExit);
        Vector2Int destination =
            exitNode.GridPosition + exitNode.LevelExitDirection.ToGridOffset();
        if (roomsByPosition.ContainsKey(destination))
            throw new InvalidOperationException(
                $"{name} level exit at {exitNode.GridPosition} points into another room.");
    }

    private static void ValidateSourceRoomData(LevelRoomNode roomNode)
    {
        RoomData roomData = roomNode.RoomPrefab.RoomData;
        if (roomNode.IsRewardRoom || roomData is RewardRoomData)
            return;

        if (roomData is not DefaultEnemiesRoomData enemiesRoomData)
            throw new InvalidOperationException(
                $"{roomNode.RoomPrefab.name} must contain DefaultEnemiesRoomData " +
                "or be marked as a reward room.");

        ValidateEnemyWaves(roomNode.RoomPrefab.name, enemiesRoomData);
    }

    private static void ValidateRuntimeRoomData(LevelRoomNode roomNode)
    {
        switch (roomNode.Room.RoomData)
        {
            case DefaultEnemiesRoomData enemiesRoomData:
                ValidateEnemyWaves(roomNode.Room.name, enemiesRoomData);
                return;
            case RewardRoomData:
                return;
            default:
                throw new InvalidOperationException(
                    $"{roomNode.Room.name} must contain DefaultEnemiesRoomData or RewardRoomData.");
        }
    }

    private static void ValidateEnemyWaves(string roomName,
        DefaultEnemiesRoomData enemiesRoomData)
    {
        if (enemiesRoomData.EnemyWavesConfiguration == null ||
            enemiesRoomData.EnemyWavesConfiguration.Length == 0)
        {
            throw new InvalidOperationException(
                $"{roomName} does not contain enemy wave configurations.");
        }
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
        var levelExits = _rooms
            .Where(roomNode => roomNode.IsLevelExit)
            .ToDictionary(roomNode => roomNode.GridPosition,
                roomNode => roomNode.LevelExitDirection);

        foreach (KeyValuePair<Vector2Int, Room> roomEntry in roomsByPosition)
        {
            foreach (RoomDirection direction in Enum.GetValues(typeof(RoomDirection)))
            {
                Vector2Int neighbourPosition =
                    roomEntry.Key + direction.ToGridOffset();
                bool hasNeighbour = roomsByPosition.TryGetValue(
                    neighbourPosition, out Room neighbourRoom);
                bool requiresExitDoor = hasNextLevel &&
                                        levelExits.TryGetValue(roomEntry.Key,
                                            out RoomDirection exitDirection) &&
                                        exitDirection == direction;

                if (!hasNeighbour && !requiresExitDoor)
                    continue;

                if (!HasDoor(roomEntry.Value, direction))
                {
                    string destination = hasNeighbour
                        ? neighbourRoom.name
                        : "the next level";
                    throw new InvalidOperationException(
                        $"{roomEntry.Value.name} does not contain a {direction} door for " +
                        $"{destination}.");
                }

                if (hasNeighbour && !HasDoor(neighbourRoom, direction.Opposite()))
                    throw new InvalidOperationException(
                        $"{neighbourRoom.name} does not contain the " +
                        $"{direction.Opposite()} opposite door.");
            }
        }
    }

    private static bool HasDoor(Room room, RoomDirection direction) =>
        room.RoomData.RoomDoors.Any(door => door.Direction == direction);

    private void ValidateAuthoringDoors(
        IReadOnlyDictionary<Vector2Int, Room> sourcesByPosition)
    {
        foreach (KeyValuePair<Vector2Int, Room> roomEntry in sourcesByPosition)
        {
            HashSet<RoomDirection> availableDirections =
                GetAvailableDirections(roomEntry.Value);

            foreach (RoomDirection direction in Enum.GetValues(typeof(RoomDirection)))
            {
                Vector2Int neighbour = roomEntry.Key + direction.ToGridOffset();
                if (sourcesByPosition.ContainsKey(neighbour) &&
                    !availableDirections.Contains(direction))
                {
                    throw new InvalidOperationException(
                        $"{roomEntry.Value.name} does not contain a {direction} door.");
                }
            }
        }
    }

    private static HashSet<RoomDirection> GetAvailableDirections(Room room)
    {
        RoomDoor[] authoredDoors = room.GetComponentsInChildren<RoomDoor>(true);
        if (authoredDoors.Length == 0)
            throw new InvalidOperationException(
                $"{room.name} does not contain authored RoomDoor objects.");

        var result = new HashSet<RoomDirection>();
        foreach (RoomDoor door in authoredDoors)
        {
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

                RoomDoor neighbourDoor = GetRequiredDoor(neighbourRoom,
                    currentDoor.Direction.Opposite());
                currentDoor.Configure(neighbourRoom, neighbourDoor);
            }
        }
    }

    private void ConfigureLevelExit(bool hasNextLevel)
    {
        if (!hasNextLevel)
            return;

        LevelRoomNode roomNode = _rooms.Single(node => node.IsLevelExit);
        RoomDoor exitDoor = GetRequiredDoor(roomNode.Room, roomNode.LevelExitDirection);
        if (exitDoor.HasRoomDestination)
            throw new InvalidOperationException(
                $"{roomNode.Room.name} level exit direction is occupied by another room.");

        exitDoor.ConfigureLevelExit();
    }

    public bool IsExitRoom(RoomData roomData) =>
        roomData != null && _rooms.Any(roomNode =>
            roomNode.IsLevelExit && roomNode.Room != null &&
            ReferenceEquals(roomNode.Room.RoomData, roomData));

    public int GetEnemyRoomIndex(RoomData roomData)
    {
        if (roomData == null)
            throw new ArgumentNullException(nameof(roomData));

        for (int i = 0; i < _rooms.Length; i++)
        {
            if (ReferenceEquals(_rooms[i]?.Room?.RoomData, roomData))
                return i;
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
            foreach (RoomDoor door in room.RoomData.RoomDoors)
                door.ClearDestination();
        }
    }

    private void ValidateConnectivity(IEnumerable<Vector2Int> roomPositions)
    {
        var positions = new HashSet<Vector2Int>(roomPositions);
        var visited = new HashSet<Vector2Int>();
        var pending = new Queue<Vector2Int>();
        pending.Enqueue(_startRoomGridPosition);

        while (pending.Count > 0)
        {
            Vector2Int position = pending.Dequeue();
            if (!visited.Add(position))
                continue;

            foreach (RoomDirection direction in Enum.GetValues(typeof(RoomDirection)))
            {
                Vector2Int neighbour = position + direction.ToGridOffset();
                if (positions.Contains(neighbour) && !visited.Contains(neighbour))
                    pending.Enqueue(neighbour);
            }
        }

        if (visited.Count != positions.Count)
            throw new InvalidOperationException(
                $"{name} contains rooms that cannot be reached from the start room.");
    }

    private static RoomDoor GetRequiredDoor(Room room, RoomDirection direction)
    {
        foreach (RoomDoor roomDoor in room.RoomData.RoomDoors)
        {
            if (roomDoor.Direction == direction)
                return roomDoor;
        }

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

        DrawRoomGizmo(_startRoomGridPosition, Color.cyan);
        if (_rooms != null)
        {
            foreach (LevelRoomNode roomNode in _rooms)
            {
                if (roomNode == null)
                    continue;

                Color color = roomNode.IsLevelExit
                    ? Color.green
                    : IsRewardDefinition(roomNode) ? Color.yellow : Color.white;
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

    private static bool IsRewardDefinition(LevelRoomNode roomNode) =>
        roomNode.IsRewardRoom || roomNode.RoomPrefab?.RoomData is RewardRoomData;

}

[Serializable]
public sealed class LevelRoomNode
{
    [FormerlySerializedAs("<Room>k__BackingField")]
    [SerializeField] private DefaultRoom _roomPrefab;
    [NonSerialized] private DefaultRoom _room;

    public DefaultRoom RoomPrefab => _roomPrefab;
    public DefaultRoom Room => _room;
    [field: SerializeField] public Vector2Int GridPosition { get; private set; }
    [field: SerializeField]
    [field: Tooltip("Convert this room instance to RewardRoomData at runtime.")]
    public bool IsRewardRoom { get; private set; }
    [field: SerializeField] public bool IsLevelExit { get; private set; }
    [field: SerializeField] public RoomDirection LevelExitDirection { get; private set; }

    public LevelRoomNode(DefaultRoom roomPrefab, Vector2Int gridPosition, bool isLevelExit,
        RoomDirection levelExitDirection, bool isRewardRoom = false)
    {
        _roomPrefab = roomPrefab;
        GridPosition = gridPosition;
        IsRewardRoom = isRewardRoom;
        IsLevelExit = isLevelExit;
        LevelExitDirection = levelExitDirection;
    }

    public void Bind(DefaultRoom room) =>
        _room = room != null
            ? room
            : throw new ArgumentNullException(nameof(room));
}
