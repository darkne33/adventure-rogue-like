using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Create LevelsConfiguration", fileName = "LevelsConfiguration", order = 0)]
public class LevelsConfiguration : ScriptableObject
{
    [field: SerializeField] public LayerMask GroundLayer { get; private set; }
    [field: SerializeField] public LayerMask ObstacleLayer { get; private set; }
    [field: SerializeField] public EnemyHealthScalingConfiguration EnemyHealthScalingConfiguration { get; private set; }
    [field: SerializeField] public EnemyRoomScalingConfiguration EnemyRoomScalingConfiguration { get; private set; }

    [Tooltip("Shared empty level prefab and enemy catalog. No authored floor layouts are used.")]
    [SerializeField] private LevelSettings _level = new();
    [SerializeField] private LevelRoomCatalog _roomCatalog;
    [SerializeField] private ProceduralLevelSettings _proceduralLevels = new();

    public LevelRoomCatalog RoomCatalog => _roomCatalog;
    public ProceduralLevelSettings ProceduralLevels => _proceduralLevels;

    // The run continues through generated floors until the player ends it.
    public bool HasLevel(int levelIndex) => levelIndex >= 0;

    public LevelSettings GetLevel(int levelIndex)
    {
        if (levelIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(levelIndex));
        if (_level?.LevelView == null || _level.EnemyFactoryConfiguration == null)
            throw new InvalidOperationException("Procedural level prefab and enemy catalog must be assigned.");
        if (_roomCatalog == null || _proceduralLevels == null)
            throw new InvalidOperationException("Room catalog and procedural generation settings must be assigned.");
        return _level;
    }

    public int GetStatGrowthStep(int levelIndex) => _proceduralLevels.GetStatGrowthStep(levelIndex);

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
}
