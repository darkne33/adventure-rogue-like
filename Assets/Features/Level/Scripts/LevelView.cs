using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelView : MonoBehaviour
{
    public const float RoomWorldSize = 160f;

    [field: SerializeField] public Room StartRoom { get; private set; }

    [SerializeField] private Vector2Int _startRoomGridPosition;
    [SerializeField] private LevelRoomNode[] _rooms;

    public Vector2Int StartRoomGridPosition => _startRoomGridPosition;
    public IReadOnlyList<LevelRoomNode> Rooms => _rooms;

    private bool _isInitialized;

    public void Configure(Room startRoom, Vector2Int startRoomGridPosition, LevelRoomNode[] rooms)
    {
        StartRoom = startRoom;
        _startRoomGridPosition = startRoomGridPosition;
        _rooms = rooms;
        _isInitialized = false;
    }

    public void Initialize(bool hasNextLevel)
    {
        if (_isInitialized)
            return;

        ResolveRoomReferences();
        Dictionary<Vector2Int, Room> roomsByPosition = ValidateAndBuildRoomMap(hasNextLevel);
        ResetDoors(roomsByPosition.Values);
        ConnectAdjacentRooms(roomsByPosition);
        ConfigureLevelExit(hasNextLevel);

        _isInitialized = true;
    }

    public void ResolveRoomReferences()
    {
        if (_rooms == null || _rooms.Length == 0)
            throw new InvalidOperationException($"{name} does not contain enemy room nodes.");

        DefaultRoom[] hierarchyRooms = GetComponentsInChildren<DefaultRoom>(true)
            .OrderBy(room => GetRoomNumber(room.name))
            .ThenBy(room => room.name)
            .ToArray();

        if (hierarchyRooms.Length != _rooms.Length)
            throw new InvalidOperationException(
                $"{name} contains {_rooms.Length} room nodes, but " +
                $"{hierarchyRooms.Length} DefaultRoom objects.");

        var assignedRooms = new HashSet<DefaultRoom>();
        for (int index = 0; index < _rooms.Length; index++)
        {
            LevelRoomNode roomNode = _rooms[index] ??
                                     throw new InvalidOperationException(
                                         $"{name} contains a missing room node.");

            DefaultRoom room = FindRoomForNode(hierarchyRooms, assignedRooms, index);
            roomNode.Bind(room);
            assignedRooms.Add(room);
            room.transform.localPosition = ToWorldPosition(roomNode.GridPosition);
        }
    }

    private Dictionary<Vector2Int, Room> ValidateAndBuildRoomMap(bool hasNextLevel)
    {
        if (StartRoom == null)
            throw new InvalidOperationException($"{name} does not have a start room.");

        ValidateRoomDoors(StartRoom);

        if (_rooms == null || _rooms.Length == 0)
            throw new InvalidOperationException($"{name} does not contain enemy rooms.");

        var roomsByPosition = new Dictionary<Vector2Int, Room>
        {
            { _startRoomGridPosition, StartRoom }
        };

        int levelExitCount = 0;
        foreach (LevelRoomNode roomNode in _rooms)
        {
            if (roomNode?.Room == null)
                throw new InvalidOperationException($"{name} contains a missing room node.");

            if (roomNode.Room.RoomData is not DefaultEnemiesRoomData)
                throw new InvalidOperationException(
                    $"{roomNode.Room.name} must contain DefaultEnemiesRoomData.");

            ValidateRoomDoors(roomNode.Room);

            if (!roomsByPosition.TryAdd(roomNode.GridPosition, roomNode.Room))
                throw new InvalidOperationException(
                    $"{name} contains more than one room at {roomNode.GridPosition}.");

            if (roomNode.IsLevelExit)
                levelExitCount++;
        }

        if (levelExitCount != 1)
            throw new InvalidOperationException(
                $"{name} must contain exactly one exit room.");

        ValidateConnectivity(roomsByPosition);
        return roomsByPosition;
    }

    private static void ValidateRoomDoors(Room room)
    {
        if (room.RoomData?.RoomDoors == null || room.RoomData.RoomDoors.Length == 0)
            throw new InvalidOperationException($"{room.name} does not have configured doors.");

        var directions = new HashSet<RoomDirection>();
        foreach (RoomDoor roomDoor in room.RoomData.RoomDoors)
        {
            if (roomDoor == null)
                throw new InvalidOperationException($"{room.name} contains a missing door.");

            if (!directions.Add(roomDoor.Direction))
                throw new InvalidOperationException(
                    $"{room.name} contains duplicate {roomDoor.Direction} doors.");
        }
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

        foreach (LevelRoomNode roomNode in _rooms)
        {
            if (!roomNode.IsLevelExit)
                continue;

            RoomDoor exitDoor = GetRequiredDoor(roomNode.Room, roomNode.LevelExitDirection);
            if (exitDoor.HasRoomDestination)
                throw new InvalidOperationException(
                    $"{roomNode.Room.name} level exit direction is occupied by another room.");

            exitDoor.ConfigureLevelExit();
            return;
        }
    }

    public bool IsExitRoom(RoomData roomData) =>
        roomData != null && _rooms.Any(roomNode =>
            roomNode.IsLevelExit && ReferenceEquals(roomNode.Room.RoomData, roomData));

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

    private static void ResetDoors(IEnumerable<Room> rooms)
    {
        foreach (Room room in rooms)
        {
            foreach (RoomDoor door in room.RoomData.RoomDoors)
                door.ClearDestination();
        }
    }

    private void ValidateConnectivity(IReadOnlyDictionary<Vector2Int, Room> roomsByPosition)
    {
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
                if (roomsByPosition.ContainsKey(neighbour) && !visited.Contains(neighbour))
                    pending.Enqueue(neighbour);
            }
        }

        if (visited.Count != roomsByPosition.Count)
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

    private static DefaultRoom FindRoomForNode(IReadOnlyList<DefaultRoom> rooms,
        ISet<DefaultRoom> assignedRooms, int nodeIndex)
    {
        int expectedRoomNumber = nodeIndex + 1;
        foreach (DefaultRoom room in rooms)
        {
            if (!assignedRooms.Contains(room) && GetRoomNumber(room.name) == expectedRoomNumber)
                return room;
        }

        foreach (DefaultRoom room in rooms)
        {
            if (!assignedRooms.Contains(room))
                return room;
        }

        throw new InvalidOperationException(
            $"Could not resolve a DefaultRoom for node {nodeIndex}.");
    }

    private static int GetRoomNumber(string roomName)
    {
        int separatorIndex = roomName.LastIndexOf('_');
        return separatorIndex >= 0 &&
               int.TryParse(roomName[(separatorIndex + 1)..], out int roomNumber)
            ? roomNumber
            : int.MaxValue;
    }

    private static Vector3 ToWorldPosition(Vector2Int gridPosition) =>
        new(gridPosition.x * RoomWorldSize, 0f, gridPosition.y * RoomWorldSize);
}

[Serializable]
public sealed class LevelRoomNode
{
    [field: SerializeField] public DefaultRoom Room { get; private set; }
    [field: SerializeField] public Vector2Int GridPosition { get; private set; }
    [field: SerializeField] public bool IsLevelExit { get; private set; }
    [field: SerializeField] public RoomDirection LevelExitDirection { get; private set; }

    public LevelRoomNode(DefaultRoom room, Vector2Int gridPosition, bool isLevelExit,
        RoomDirection levelExitDirection)
    {
        Room = room;
        GridPosition = gridPosition;
        IsLevelExit = isLevelExit;
        LevelExitDirection = levelExitDirection;
    }

    public void Bind(DefaultRoom room) =>
        Room = room != null
            ? room
            : throw new ArgumentNullException(nameof(room));
}
