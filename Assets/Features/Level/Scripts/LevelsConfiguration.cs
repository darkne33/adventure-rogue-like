using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Create LevelsConfiguration", fileName = "LevelsConfiguration", order = 0)]
public class LevelsConfiguration : ScriptableObject
{
    [field: SerializeField] public LayerMask GroundLayer { get; private set; }
    [field: SerializeField] public LayerMask ObstacleLayer { get; private set; }
    [field: SerializeField] public EnemyHealthScalingConfiguration EnemyHealthScalingConfiguration { get; private set; }
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

    public EnemyHealthScalingConfiguration GetEnemyHealthScalingConfiguration()
    {
        if (EnemyHealthScalingConfiguration == null)
            throw new InvalidOperationException("Enemy health scaling configuration is missing.");

        return EnemyHealthScalingConfiguration;
    }
}

[Serializable]
public class LevelSettings
{
    [field: SerializeField] public EnemyFactoryConfiguration EnemyFactoryConfiguration { get; private set; }
    [field: SerializeField] public LevelView LevelView { get; private set; }

    [field: SerializeField, Min(1)]
    [field: Tooltip("Number of enemies spawned when entering the first combat room.")]
    public int StartEnemies { get; private set; } = 1;

    [field: SerializeField, Min(1)]
    [field: Tooltip("Total number of enemies spawned in the first combat room.")]
    public int AllEnemiesInRoom { get; private set; } = 1;

    [field: SerializeField, Min(0)]
    [field: Tooltip("Amount added to both enemy counts for each next combat room.")]
    public int CountIncrease { get; private set; } = 2;

    public int GetStartEnemyCount(int roomIndex) =>
        Mathf.Max(1, StartEnemies) + Mathf.Max(0, roomIndex) * Mathf.Max(0, CountIncrease);

    public int GetAllEnemyCount(int roomIndex) =>
        Mathf.Max(GetStartEnemyCount(roomIndex),
            Mathf.Max(1, AllEnemiesInRoom) +
            Mathf.Max(0, roomIndex) * Mathf.Max(0, CountIncrease));
}
