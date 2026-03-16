using System;
using UnityEngine;
using UnityEngine.Serialization;

public class Room : MonoBehaviour
{
    [field: SubclassSelector]
    [field: SerializeReference]
    public RoomData RoomData { get; private set; }
}

[Serializable]
public class RoomData
{
    [field: SerializeField] public RoomDoor[] RoomDoors { get; set; }
}

[Serializable]
public class DefaultEnemiesRoomData : RoomData
{
    [field: SerializeField] public EnemyWavesConfiguration[] EnemyWavesConfiguration { get; private set; }
}

[Serializable]
public class StartRoomData : RoomData
{
    [field: SerializeField] public Transform StartPoint;
}