using System;
using UnityEngine;

[Serializable]
public sealed class OnlyRelicRoomData : RoomData
{
    [field: SerializeField] public Transform RelicSpawnPoint { get; private set; }
    [field: SerializeField, Min(0)] public int KeyPrice { get; private set; }

    public bool RequiresKey => KeyPrice > 0;
    public bool IsCompleted { get; private set; }
    public bool IsUnlocked { get; private set; }

    public void ConfigureEntrance(bool requiresKey) =>
        KeyPrice = requiresKey ? 1 : 0;

    public void Unlock() => IsUnlocked = true;

    public void MarkCompleted() => IsCompleted = true;

    public void ResetProgress()
    {
        IsCompleted = false;
        IsUnlocked = false;
    }
}
