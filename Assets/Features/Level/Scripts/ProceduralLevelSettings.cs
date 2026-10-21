using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ProceduralLevelSettings
{
    [Header("Layout")]
    [Tooltip("Maximum attempts to find a connected floor with enough dead ends and compatible prefabs.")]
    [SerializeField, Range(1, 8192)] private int _generationAttempts = 2048;

    [Header("Only One Relic Room")]
    [Tooltip("Chance that an Only One Relic room requires one key. Rolled once before layout retries.")]
    [SerializeField, Range(0f, 1f)] private float _onlyRelicKeyedChance = 0.7f;

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

    public bool RollOnlyRelicRequiresKey() =>
        UnityEngine.Random.value < Mathf.Clamp01(_onlyRelicKeyedChance);

    public List<RoomType> RollSpecialRooms(int levelIndex, int goldCount)
    {
        var types = new List<RoomType>();

        // Isaac's first three chapters contain six floors, through Depths II.
        // This is the base rule; the current relic catalog has no late-floor room modifiers.
        // Shop is the keyed wheel. Treasure Room uses its own free/keyed entrance state.
        // Only Relic remains a separate room with its existing first-floor availability.
        if (levelIndex < 6)
        {
            if (goldCount >= 10)
                types.Add(RoomType.Shop);
            types.Add(RoomType.Treasure);
            if (levelIndex == 0)
                types.Add(RoomType.OnlyRelic);
        }

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
