using System;
using System.Collections.Generic;

public class RogueLikeRuntimeDataService : IRogueLikeRuntimeDataService
{
    public int CurrentIndexLevel { get; set; }
    public RoomData CurrentRoomData { get; private set; }
    public int VisitedRoomsCount => _visitedRooms.Count;

    private readonly HashSet<RoomData> _visitedRooms = new();
    private readonly Dictionary<RoomData, int> _combatRoomVisitIndices = new();

    public event Action<RoomData, RoomData> RoomChanged;

    public bool HasVisitedRoom(RoomData roomData) =>
        roomData != null && _visitedRooms.Contains(roomData);

    public int GetCombatProgressIndex(RoomData roomData)
    {
        if (roomData == null)
            throw new ArgumentNullException(nameof(roomData));

        if (!_combatRoomVisitIndices.TryGetValue(roomData, out int roomIndex))
            throw new InvalidOperationException(
                "Combat room must be visited before its progression index can be read.");

        return roomIndex;
    }

    public void SetCurrentRoomData(RoomData roomData)
    {
        if (roomData == null)
            throw new ArgumentNullException(nameof(roomData));

        RoomData previousRoom = CurrentRoomData;
        CurrentRoomData = roomData;
        if (_visitedRooms.Add(roomData) && roomData is DefaultEnemiesRoomData)
            _combatRoomVisitIndices.Add(roomData, _combatRoomVisitIndices.Count);
        RoomChanged?.Invoke(previousRoom, CurrentRoomData);
    }
}
