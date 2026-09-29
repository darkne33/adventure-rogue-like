using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Create LevelsConfiguration", fileName = "LevelsConfiguration", order = 0)]
public class LevelsConfiguration : ScriptableObject
{
    public const int AuthoredLevelCount = 3;

    [field: SerializeField] public LayerMask GroundLayer { get; private set; }
    [field: SerializeField] public LayerMask ObstacleLayer { get; private set; }
    [field: SerializeField] public EnemyHealthScalingConfiguration EnemyHealthScalingConfiguration { get; private set; }
    [field: SerializeField] public EnemyRoomScalingConfiguration EnemyRoomScalingConfiguration { get; private set; }
    [field: SerializeField] public List<LevelSettings> Levels { get; private set; }

    [Tooltip("Levels one through three always generate a new layout using their corresponding example.")]
    [SerializeField] private OpeningLevelGenerationSettings _openingLevels = new();
    public OpeningLevelGenerationSettings OpeningLevels => _openingLevels;

    [SerializeField] private ProceduralLevelSettings _proceduralLevels = new();
    public ProceduralLevelSettings ProceduralLevels => _proceduralLevels;

    public bool IsProceduralLevel(int levelIndex) =>
        _proceduralLevels != null && _proceduralLevels.Enabled && levelIndex >= AuthoredLevelCount;

    public int GetGeneratedLevelNumber(int levelIndex) =>
        IsProceduralLevel(levelIndex) ? levelIndex - AuthoredLevelCount + 1 : 0;

    public bool HasLevel(int levelIndex) =>
        HasAuthoredLevel(IsProceduralLevel(levelIndex) ? AuthoredLevelCount - 1 : levelIndex);

    private bool HasAuthoredLevel(int levelIndex) =>
        Levels != null && levelIndex >= 0 && levelIndex < Levels.Count && Levels[levelIndex] != null;

    public LevelSettings GetLevel(int levelIndex)
    {
        // Generated levels reuse the loaded level-three shell, room variants,
        // and enemy catalog. Only their runtime room graph is replaced.
        if (IsProceduralLevel(levelIndex))
            levelIndex = AuthoredLevelCount - 1;

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

    public int GetCombatProgressIndex(int levelIndex, LevelView level, RoomData roomData) =>
        level.CombatProgressOffset + level.GetEnemyRoomIndex(roomData);

    public int GetCombatProgressOffset(int levelIndex,
        IReadOnlyDictionary<int, int> openingCombatRoomCounts = null)
    {
        long index = 0;
        int authoredCount = IsProceduralLevel(levelIndex) ? AuthoredLevelCount : levelIndex;
        for (int i = 0; i < authoredCount; i++)
        {
            if (i < AuthoredLevelCount)
            {
                // Runtime lengths belong to the current run, not the loaded config asset.
                // Use the example's nominal length only when starting at a later level directly.
                index += openingCombatRoomCounts != null &&
                         openingCombatRoomCounts.TryGetValue(i, out int count)
                    ? count
                    : _openingLevels.GetDefaultCombatRoomsToExit(GetLevel(i).LevelView);
            }
            else
            {
                index += GetLevel(i).LevelView.GetCombatRoomsToExit();
            }
        }

        if (IsProceduralLevel(levelIndex))
            index += _proceduralLevels.GetPreviousCombatRoomCount(levelIndex - AuthoredLevelCount);
        return (int)Math.Min(int.MaxValue, index);
    }
}

[Serializable]
public class LevelSettings
{
    [field: SerializeField] public EnemyFactoryConfiguration EnemyFactoryConfiguration { get; private set; }
    [field: SerializeField] public LevelView LevelView { get; private set; }
}
