using System;
using UnityEngine;

[Flags]
public enum BloodRoomReward
{
    Bag = 1,
    Chest = 2,
    BagAndChest = Bag | Chest
}

[Serializable]
public sealed class BloodRoomData : RoomData
{
    [field: SerializeField] public Transform RewardSpawnPoint { get; private set; }
    [field: SerializeField] public Transform BagSpawnPoint { get; private set; }
    [field: SerializeField] public Transform ChestSpawnPoint { get; private set; }
    [field: SerializeField, Min(0f)] public float BagWeight { get; private set; } = 60f;
    [field: SerializeField, Min(0f)] public float ChestWeight { get; private set; } = 30f;
    [field: SerializeField, Min(0f)] public float BagAndChestWeight { get; private set; } = 10f;

    public bool HasSpawnedReward => _reward.HasValue && HasSpawned(_reward.Value);
    public bool IsCompleted => _reward.HasValue && (_collectedRewards & _reward.Value) == _reward.Value;
    private BloodRoomReward? _reward;
    private BloodRoomReward _spawnedRewards;
    private BloodRoomReward _collectedRewards;

    public BloodRoomReward GetReward()
    {
        if (_reward.HasValue)
            return _reward.Value;

        float bag = Mathf.Max(0f, BagWeight);
        float chest = Mathf.Max(0f, ChestWeight);
        float bagAndChest = Mathf.Max(0f, BagAndChestWeight);
        float total = bag + chest + bagAndChest;
        if (total <= 0f)
            throw new InvalidOperationException("Blood room reward weights must have a positive total.");

        float roll = UnityEngine.Random.Range(0f, total);
        _reward = roll < bag ? BloodRoomReward.Bag
            : roll < bag + chest ? BloodRoomReward.Chest
            : bagAndChest > 0f ? BloodRoomReward.BagAndChest
            : chest > 0f ? BloodRoomReward.Chest : BloodRoomReward.Bag;
        return _reward.Value;
    }

    public bool HasSpawned(BloodRoomReward reward) => (_spawnedRewards & reward) == reward;
    public void MarkRewardSpawned(BloodRoomReward reward) => _spawnedRewards |= reward;
    public void MarkRewardCollected(BloodRoomReward reward) => _collectedRewards |= reward;

    public void ResetProgress()
    {
        _spawnedRewards = default;
        _collectedRewards = default;
        _reward = null;
    }
}
