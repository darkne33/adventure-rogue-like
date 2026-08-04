using System;
using UnityEngine;

public class Room : MonoBehaviour
{
    [field: SubclassSelector]
    [field: SerializeReference]
    public RoomData RoomData { get; private set; }

    internal void SetRoomData(RoomData roomData) =>
        RoomData = roomData ?? throw new ArgumentNullException(nameof(roomData));

#if UNITY_EDITOR
    public void SetEditorRoomData(RoomData roomData) =>
        SetRoomData(roomData);
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
    public EnemyRoomConfiguration Configuration { get; private set; }
    public bool IsCompleted { get; private set; }

    internal void Configure(EnemyRoomConfiguration configuration) =>
        Configuration = configuration != null
            ? configuration
            : throw new ArgumentNullException(nameof(configuration));

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
public class ShopRoomData : RoomData
{
}

[Serializable]
public class StartRoomData : RoomData
{
    [field: SerializeField] public Transform StartPoint;
}
