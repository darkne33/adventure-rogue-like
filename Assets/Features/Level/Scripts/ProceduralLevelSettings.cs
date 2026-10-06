using System;
using UnityEngine;

[Serializable]
public sealed class ProceduralLevelSettings
{
    [Header("Floor Size")]
    [Tooltip("Total combat rooms on the first floor, including its boss.")]
    [SerializeField, Min(4)] private int _initialCombatRooms = 8;
    [SerializeField, Min(4)] private int _maximumCombatRooms = 14;
    [SerializeField, Min(1)] private int _levelsPerAdditionalRoom = 2;

    [Header("Layout")]
    [Tooltip("Share of combat rooms on the route to the boss, including the boss.")]
    [SerializeField, Range(0.5f, 0.85f)] private float _mainPathCombatFraction = 0.7f;
    [Tooltip("Maximum number of consecutive side-branch combat rooms.")]
    [SerializeField, Range(1, 3)] private int _maximumBranchCombatRooms = 2;
    [Tooltip("The first combat branch must start within this many fights from the start.")]
    [SerializeField, Range(1, 3)] private int _firstBranchMaximumDepth = 2;
    [Tooltip("Room coordinates range from minus this value to plus this value.")]
    [SerializeField, Range(4, 12)] private int _gridRadius = 6;
    [SerializeField, Range(1, 512)] private int _generationAttempts = 128;

    [Header("Optional Rooms")]
    [Tooltip("Relative weights for exactly 0, 1, 2 or 3 reward rooms (X, Y, Z, W).")]
    [SerializeField] private Vector4 _rewardRoomCountWeights = Vector4.one;
    [Tooltip("Separate relic rooms added to every floor, in addition to chest reward rooms.")]
    [SerializeField, Min(0)] private int _onlyRelicRoomCount = 1;
    [SerializeField, Range(0f, 100f)] private float _shopChancePercent = 50f;

    [Header("Existing Additional Stat Growth")]
    [Tooltip("One-based floor number at which additional health/damage growth starts.")]
    [SerializeField, Min(1)] private int _statGrowthStartLevel = 4;
    [SerializeField, Min(0f)] private float _healthGrowthPerLevel = 0.15f;
    [SerializeField, Min(0f)] private float _damageGrowthPerLevel = 0.05f;

    public int OnlyRelicRoomCount => Mathf.Max(0, _onlyRelicRoomCount);
    public int MaximumBranchCombatRooms => Mathf.Clamp(_maximumBranchCombatRooms, 1, 3);
    public int FirstBranchMaximumDepth => Mathf.Clamp(_firstBranchMaximumDepth, 1, 3);
    public int GridRadius => Mathf.Clamp(_gridRadius, 4, 12);
    public int GenerationAttempts => Mathf.Clamp(_generationAttempts, 1, 512);

    public int GetCombatRoomCount(int levelIndex)
    {
        int initial = Mathf.Max(4, _initialCombatRooms);
        int maximum = Mathf.Max(initial, _maximumCombatRooms);
        int growth = Mathf.Max(0, levelIndex) / Mathf.Max(1, _levelsPerAdditionalRoom);
        return initial + Mathf.Min(maximum - initial, growth);
    }

    public int GetMainPathCombatRoomCount(int combatRooms) =>
        Mathf.Clamp(Mathf.RoundToInt(combatRooms * Mathf.Clamp(_mainPathCombatFraction, 0.5f, 0.85f)),
            3, combatRooms - 1);

    public int RollRewardRoomCount()
    {
        float total = 0f;
        for (int i = 0; i < 4; i++)
            total += Mathf.Max(0f, _rewardRoomCountWeights[i]);
        if (total <= 0f)
            return 0;

        float roll = UnityEngine.Random.value * total;
        int lastAllowedCount = 0;
        for (int i = 0; i < 4; i++)
        {
            float weight = Mathf.Max(0f, _rewardRoomCountWeights[i]);
            if (weight <= 0f)
                continue;
            lastAllowedCount = i;
            if (roll < weight)
                return i;
            roll -= weight;
        }
        return lastAllowedCount;
    }

    public bool RollShop() => _shopChancePercent >= 100f ||
        (_shopChancePercent > 0f && UnityEngine.Random.value < _shopChancePercent / 100f);

    public int GetStatGrowthStep(int levelIndex) =>
        Mathf.Max(0, levelIndex - Mathf.Max(1, _statGrowthStartLevel) + 2);

    public int ScaleHealth(int baseHealth, int growthStep) =>
        ScaleStat(baseHealth, growthStep, _healthGrowthPerLevel);

    public int ScaleDamage(int baseDamage, int growthStep) =>
        ScaleStat(baseDamage, growthStep, _damageGrowthPerLevel);

    private static int ScaleStat(int value, int growthStep, float growth)
    {
        if (growthStep <= 0)
            return value;
        double multiplier = 1d + growthStep * (double)Mathf.Max(0f, growth);
        return (int)Math.Max(1d, Math.Min(int.MaxValue, Math.Ceiling(value * multiplier)));
    }
}
