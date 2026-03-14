public interface IRogueLikeRuntimeDataService
{
    public int CurrentIndexLevel { get; set; }
    public RoomData CurrentRoomData { get; }
    public void SetCurrentRoomData(RoomData roomData);
}