using System;
using UnityEngine;

[Serializable]
public sealed class OnlyRelicRoomData : RoomData
{
    [field: SerializeField] public Transform RelicSpawnPoint { get; private set; }

    public bool IsCompleted { get; private set; }

    public void MarkCompleted() => IsCompleted = true;

    public void ResetProgress() => IsCompleted = false;
}
