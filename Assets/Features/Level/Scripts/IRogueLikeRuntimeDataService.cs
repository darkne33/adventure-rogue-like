using System;

public interface IRogueLikeRuntimeDataService
{
    int CurrentIndexLevel { get; set; }
    RoomData CurrentRoomData { get; }
    event Action<RoomData, RoomData> RoomChanged;
    void SetCurrentRoomData(RoomData roomData);
}
