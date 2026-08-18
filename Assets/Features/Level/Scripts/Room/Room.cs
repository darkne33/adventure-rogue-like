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
    public EnemyRoomSettings EnemySettings { get; private set; }
    public bool IsCompleted { get; private set; }

    internal void Configure(EnemyRoomSettings enemySettings) =>
        EnemySettings = enemySettings ?? throw new ArgumentNullException(nameof(enemySettings));

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
    [field: SerializeField]
    [field: Tooltip("Element 0 contains points for one chest, element 1 for two chests, and so on.")]
    public RewardChestSpawnPoints[] ChestSpawnPointsByCount { get; private set; } =
        Array.Empty<RewardChestSpawnPoints>();

    public bool IsCompleted { get; private set; }

    public int GetChestCount()
    {
        int min = Mathf.Min(MinChests, MaxChests);
        int max = Mathf.Max(MinChests, MaxChests);
        return UnityEngine.Random.Range(min, max + 1);
    }

    public Transform GetChestSpawnPoint(int chestCount, int chestIndex)
    {
        int pointsIndex = chestCount - 1;
        if (pointsIndex < 0 || ChestSpawnPointsByCount == null ||
            pointsIndex >= ChestSpawnPointsByCount.Length)
            return null;

        RewardChestSpawnPoints spawnPoints = ChestSpawnPointsByCount[pointsIndex];
        if (spawnPoints?.Points == null || chestIndex < 0 || chestIndex >= spawnPoints.Points.Length)
            return null;

        return spawnPoints.Points[chestIndex];
    }

    public void MarkCompleted() =>
        IsCompleted = true;

    public void ResetProgress() =>
        IsCompleted = false;
}

[Serializable]
public sealed class RewardChestSpawnPoints
{
    [field: SerializeField] public Transform[] Points { get; private set; } = Array.Empty<Transform>();
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
