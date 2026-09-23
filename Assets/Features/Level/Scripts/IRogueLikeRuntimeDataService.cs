using System;

public interface IRogueLikeRuntimeDataService
{
    int CurrentIndexLevel { get; set; }
    RoomData CurrentRoomData { get; }
    int VisitedRoomsCount { get; }
    event Action<RoomData, RoomData> RoomChanged;
    bool HasVisitedRoom(RoomData roomData);
    void SetCurrentRoomData(RoomData roomData);
}
