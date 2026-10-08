using System;
using UnityEngine;

public enum BloodRoomReward
{
    Bag,
    Chest,
    Relic
}

[Serializable]
public sealed class BloodRoomData : RoomData
{
    [field: SerializeField] public Transform RewardSpawnPoint { get; private set; }
    [field: SerializeField, Min(0f)] public float BagWeight { get; private set; } = 60f;
    [field: SerializeField, Min(0f)] public float ChestWeight { get; private set; } = 20f;
    [field: SerializeField, Min(0f)] public float RelicWeight { get; private set; } = 20f;

    public bool HasSpawnedReward { get; private set; }
    public bool IsCompleted { get; private set; }
    private BloodRoomReward? _reward;

    public BloodRoomReward GetReward()
    {
        if (_reward.HasValue)
            return _reward.Value;

        float bag = Mathf.Max(0f, BagWeight);
        float chest = Mathf.Max(0f, ChestWeight);
        float relic = Mathf.Max(0f, RelicWeight);
        float total = bag + chest + relic;
        if (total <= 0f)
            throw new InvalidOperationException("Blood room reward weights must have a positive total.");

        float roll = UnityEngine.Random.Range(0f, total);
        _reward = roll < bag ? BloodRoomReward.Bag
            : roll < bag + chest ? BloodRoomReward.Chest
            : relic > 0f ? BloodRoomReward.Relic
            : chest > 0f ? BloodRoomReward.Chest : BloodRoomReward.Bag;
        return _reward.Value;
    }

    public void MarkRewardSpawned() => HasSpawnedReward = true;
    public void MarkCompleted() => IsCompleted = true;

    public void ResetProgress()
    {
        HasSpawnedReward = false;
        IsCompleted = false;
        _reward = null;
    }
}
