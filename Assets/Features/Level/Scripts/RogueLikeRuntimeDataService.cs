public class RogueLikeRuntimeDataService : IRogueLikeRuntimeDataService
{
    public int CurrentIndexLevel { get; set; }
    public RoomData CurrentRoomData { get; private set; }
    
    public void SetCurrentRoomData(RoomData roomData) => 
        CurrentRoomData = roomData;
}