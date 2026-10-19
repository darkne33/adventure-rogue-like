using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ProceduralLevelSettings
{
    [Header("Layout")]
    [Tooltip("Maximum attempts to find a connected floor with enough dead ends and compatible prefabs.")]
    [SerializeField, Range(1, 8192)] private int _generationAttempts = 2048;

    [Header("Existing Additional Stat Growth")]
    [Tooltip("One-based floor number at which additional health/damage growth starts.")]
    [SerializeField, Min(1)] private int _statGrowthStartLevel = 4;
    [SerializeField, Min(0f)] private float _healthGrowthPerLevel = 0.15f;
    [SerializeField, Min(0f)] private float _damageGrowthPerLevel = 0.05f;

    public int GridRadius => 6;
    public int GenerationAttempts => Mathf.Clamp(_generationAttempts, 1, 8192);

    public int RollRoomCount(int levelIndex)
    {
        if (levelIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(levelIndex));

        // Rebirth normal floor: min(20, random(0, 1) + 5 + floor(depth * 10 / 3)).
        // This counts the whole floor plan, including rooms later assigned special types.
        long depth = (long)levelIndex + 1;
        return (int)Math.Min(20L, UnityEngine.Random.Range(0, 2) + 5L + depth * 10L / 3L);
    }

    public int GetMinimumDeadEnds(int levelIndex) => levelIndex == 0 ? 5 : 6;

    public List<RoomType> RollSpecialRooms(int levelIndex)
    {
        // Shops and keyed-chest rooms are mandatory on every floor of the endless run.
        var types = new List<RoomType> { RoomType.Shop };

        // Standalone relic rooms retain their existing first-six-floor availability.
        if (levelIndex < 6)
            types.Add(RoomType.OnlyRelic);

        types.Add(RoomType.Reward);

        // Base Curse Room roll, through chapter 5. This game has no Devil Room visit state.
        if (levelIndex < 9 && UnityEngine.Random.Range(0, 2) == 0)
            types.Add(RoomType.Blood);

        return types;
    }

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
