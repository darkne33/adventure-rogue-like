using System;
using UnityEngine;

public class Room : MonoBehaviour
{
    [field: SubclassSelector][field: SerializeReference] public RoomData RoomData { get; private set; }
}

[Serializable]
public class RoomData
{
    [field: SerializeField] public GameObject[] DoorsPrefab { get; set; }
}

[Serializable]
public class DefaultEnemiesRoomData : RoomData
{
    [field: SerializeField] public EnemyWavesConfiguration[] EnemyWavesConfiguration { get; private set; }
}