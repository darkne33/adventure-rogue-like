using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Create LevelsConfiguration", fileName = "LevelsConfiguration", order = 0)]
public class LevelsConfiguration : ScriptableObject
{
    [field: SerializeField] public LayerMask GroundLayer { get; private set; }
    [field: SerializeField] public LayerMask ObstacleLayer { get; private set; }
    [field: SerializeField] public EnemyWaveScalingConfiguration EnemyWaveScalingConfiguration { get; private set; }
    [field: SerializeField] public EnemyHealthScalingConfiguration EnemyHealthScalingConfiguration { get; private set; }
    [field: SerializeField] public EnemyTimedSpawnScalingConfiguration EnemyTimedSpawnScalingConfiguration { get; private set; }
    [field: SerializeField] public List<LevelSettings> Levels { get; private set; }

    public bool HasLevel(int levelIndex) =>
        Levels != null && levelIndex >= 0 && levelIndex < Levels.Count && Levels[levelIndex] != null;

    public LevelSettings GetLevel(int levelIndex)
    {
        if (Levels == null || levelIndex < 0 || levelIndex >= Levels.Count)
            throw new ArgumentOutOfRangeException(nameof(levelIndex), levelIndex,
                $"Level index must be between 0 and {(Levels?.Count ?? 0) - 1}.");

        LevelSettings level = Levels[levelIndex];
        if (level == null)
            throw new InvalidOperationException($"Level configuration at index {levelIndex} is null.");

        return level;
    }

    public EnemyWaveScalingConfiguration GetEnemyWaveScalingConfiguration()
    {
        if (EnemyWaveScalingConfiguration == null)
            throw new InvalidOperationException("Enemy wave scaling configuration is missing.");

        return EnemyWaveScalingConfiguration;
    }

    public EnemyHealthScalingConfiguration GetEnemyHealthScalingConfiguration()
    {
        if (EnemyHealthScalingConfiguration == null)
            throw new InvalidOperationException("Enemy health scaling configuration is missing.");

        return EnemyHealthScalingConfiguration;
    }

    public EnemyTimedSpawnScalingConfiguration GetEnemyTimedSpawnScalingConfiguration()
    {
        if (EnemyTimedSpawnScalingConfiguration == null)
            throw new InvalidOperationException("Enemy timed spawn scaling configuration is missing.");

        return EnemyTimedSpawnScalingConfiguration;
    }
}

[Serializable]
public class LevelSettings
{
    [field: SerializeField] public EnemyFactoryConfiguration EnemyFactoryConfiguration { get; private set; }
    [field: SerializeField] public LevelView LevelView { get; private set; }
}
