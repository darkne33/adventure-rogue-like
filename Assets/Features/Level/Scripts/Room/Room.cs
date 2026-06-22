using System;
using UnityEngine;

public class Room : MonoBehaviour
{
    [field: SubclassSelector]
    [field: SerializeReference]
    public RoomData RoomData { get; private set; }

#if UNITY_EDITOR
    public void SetEditorRoomData(RoomData roomData) =>
        RoomData = roomData ?? throw new ArgumentNullException(nameof(roomData));
#endif
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
    [field: SerializeField, Min(0f)] public float TimedSpawnDuration { get; private set; } = 10f;
    [field: SerializeField, Min(0.1f)] public float AdditionalSpawnInterval { get; private set; } = 2f;
    [field: SerializeField, Min(0)] public int AdditionalEnemiesPerSpawn { get; private set; } = 1;

    public bool IsCompleted { get; private set; }

    public void MarkCompleted() =>
        IsCompleted = true;

    public void ResetProgress() =>
        IsCompleted = false;
}

[Serializable]
public class RewardRoomData : RoomData
{
    [field: SerializeField, Min(1)] public int MinChests { get; private set; } = 1;
    [field: SerializeField, Min(1)] public int MaxChests { get; private set; } = 2;

    public bool IsCompleted { get; private set; }

    public int GetChestCount()
    {
        int min = Mathf.Min(MinChests, MaxChests);
        int max = Mathf.Max(MinChests, MaxChests);
        return UnityEngine.Random.Range(min, max + 1);
    }

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
