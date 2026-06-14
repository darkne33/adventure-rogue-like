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

    public bool IsCompleted { get; private set; }

    public void MarkCompleted() =>
        IsCompleted = true;

    public void ResetProgress() =>
        IsCompleted = false;
}

[Serializable]
public class StartRoomData : RoomData
{
    [field: SerializeField] public Transform StartPoint;
}
