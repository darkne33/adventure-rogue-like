using System;

public class RogueLikeRuntimeDataService : IRogueLikeRuntimeDataService
{
    public int CurrentIndexLevel { get; set; }
    public RoomData CurrentRoomData { get; private set; }

    public event Action<RoomData, RoomData> RoomChanged;

    public void SetCurrentRoomData(RoomData roomData)
    {
        if (roomData == null)
            throw new ArgumentNullException(nameof(roomData));

        RoomData previousRoom = CurrentRoomData;
        CurrentRoomData = roomData;
        RoomChanged?.Invoke(previousRoom, CurrentRoomData);
    }
}
