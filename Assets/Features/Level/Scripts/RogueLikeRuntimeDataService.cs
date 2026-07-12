using System;
using System.Collections.Generic;

public class RogueLikeRuntimeDataService : IRogueLikeRuntimeDataService
{
    public int CurrentIndexLevel { get; set; }
    public RoomData CurrentRoomData { get; private set; }
    public int VisitedRoomsCount => _visitedRooms.Count;

    private readonly HashSet<RoomData> _visitedRooms = new();

    public event Action<RoomData, RoomData> RoomChanged;

    public void SetCurrentRoomData(RoomData roomData)
    {
        if (roomData == null)
            throw new ArgumentNullException(nameof(roomData));

        RoomData previousRoom = CurrentRoomData;
        CurrentRoomData = roomData;
        _visitedRooms.Add(roomData);
        RoomChanged?.Invoke(previousRoom, CurrentRoomData);
    }
}
