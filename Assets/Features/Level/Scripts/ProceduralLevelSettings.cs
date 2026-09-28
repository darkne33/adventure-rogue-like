using System;
using UnityEngine;

[Serializable]
public sealed class ProceduralLevelSettings
{
    [SerializeField] private bool _enabled = true;
    [Tooltip("Combat rooms on level four, including its final boss room.")]
    [SerializeField, Min(3)] private int _initialCombatRooms = 8;
    [SerializeField, Min(3)] private int _maximumCombatRooms = 14;
    [Tooltip("Add one combat room after this many generated levels.")]
    [SerializeField, Min(1)] private int _levelsPerAdditionalRoom = 2;
    [SerializeField, Min(0)] private int _rewardRooms = 3;
    [SerializeField, Min(0)] private int _shopRooms = 2;
    [Tooltip("Add this fraction of base health per level after level three.")]
    [SerializeField, Min(0f)] private float _healthGrowthPerLevel = 0.15f;
    [Tooltip("Add this fraction of base enemy damage per level after level three.")]
    [SerializeField, Min(0f)] private float _damageGrowthPerLevel = 0.05f;

    public bool Enabled => _enabled;
    public int RewardRooms => Mathf.Max(0, _rewardRooms);
    public int ShopRooms => Mathf.Max(0, _shopRooms);

    public int GetCombatRoomCount(int generatedLevelIndex)
    {
        int initial = Mathf.Max(3, _initialCombatRooms);
        int maximum = Mathf.Max(initial, _maximumCombatRooms);
        int growth = Mathf.Max(0, generatedLevelIndex) / Mathf.Max(1, _levelsPerAdditionalRoom);
        return initial + Mathf.Min(maximum - initial, growth);
    }

    public int GetPreviousCombatRoomCount(int generatedLevelIndex)
    {
        // Sum the length progression without walking every preceding level.
        long count = Mathf.Max(0, generatedLevelIndex);
        long initial = Mathf.Max(3, _initialCombatRooms);
        long growthLimit = Math.Max(initial, _maximumCombatRooms) - initial;
        long interval = Mathf.Max(1, _levelsPerAdditionalRoom);
        long growingCount = Math.Min(count, growthLimit * interval);
        long fullGroups = growingCount / interval;
        long remainder = growingCount % interval;
        long extraRooms = interval * fullGroups * (fullGroups - 1) / 2 +
                          fullGroups * remainder + (count - growingCount) * growthLimit;
        return (int)Math.Min(int.MaxValue, initial * count + extraRooms);
    }

    public int ScaleHealth(int baseHealth, int generatedLevelNumber) =>
        ScaleStat(baseHealth, generatedLevelNumber, _healthGrowthPerLevel);

    public int ScaleDamage(int baseDamage, int generatedLevelNumber) =>
        ScaleStat(baseDamage, generatedLevelNumber, _damageGrowthPerLevel);

    private static int ScaleStat(int value, int generatedLevelNumber, float growth)
    {
        if (generatedLevelNumber <= 0)
            return value;

        double multiplier = 1d + generatedLevelNumber * (double)Mathf.Max(0f, growth);
        return (int)Math.Max(1d, Math.Min(int.MaxValue, Math.Ceiling(value * multiplier)));
    }
}
