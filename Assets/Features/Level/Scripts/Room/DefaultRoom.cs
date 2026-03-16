using UnityEngine;

public class DefaultRoom : Room
{
    [field: SerializeField] public RoomDoor EnterRoom { get; private set; }
}