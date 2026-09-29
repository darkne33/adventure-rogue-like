using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Enemies/Enemy Room Scaling Configuration",
    fileName = "EnemyRoomScalingConfiguration", order = 0)]
public class EnemyRoomScalingConfiguration : ScriptableObject
{
    [Tooltip("One entry per first combat-room visit in the run. Later rooms keep the final value.")]
    [SerializeField] private int[] _startingEnemies =
        { 2, 3, 4, 5, 7, 9, 12, 14, 19, 14, 24, 24, 24, 24, 24, 24, 26, 27, 28, 30 };
    [Tooltip("Total enemies across all waves; this can exceed the simultaneous enemy limit. Later rooms keep the final value.")]
    [SerializeField] private int[] _totalEnemies =
        { 2, 3, 4, 5, 7, 9, 12, 14, 19, 14, 24, 34, 29, 38, 48, 53, 61, 70, 80, 90 };
    [Tooltip("Maximum living enemies at once, including survivors from earlier waves.")]
    [SerializeField, Min(1)] private int _maxAliveEnemies = 30;
    [Tooltip("Maximum total enemies across all waves for a Small room. Larger groups use Medium rooms.")]
    [SerializeField, Min(1)] private int _maxEnemiesInSmallRoom = 9;
    [Header("Combat Waves")]
    [Tooltip("First one-based combat-room visit where rooms can use three waves, regardless of level.")]
    [SerializeField, Min(1)] private int _firstWaveRoom = 5;
    [SerializeField, Range(0f, 100f)] private float _waveRoomChancePercent = 50f;
    [Tooltip("Relative sizes of three waves, scaled to preserve the room's total enemy count.")]
    [SerializeField] private Vector3Int[] _wavePatterns = { new(4, 5, 4), new(3, 7, 3) };
    [Tooltip("Inclusive range of surviving enemies that triggers the next wave.")]
    [SerializeField] private Vector2Int _waveRemainingEnemiesRange = new(1, 2);
    [Tooltip("Maximum enemies prepared and spawned in one group of a wave.")]
    [SerializeField, Min(1)] private int _spawnBatchSize = 10;
    [Tooltip("Delay between spawn groups in seconds of game time.")]
    [SerializeField, Min(0f)] private float _spawnBatchDelay = 1f;
    [SerializeField, Min(1)] private int _firstEliteRoom = 4;
    [SerializeField, Min(1)] private int _eliteRoomInterval = 3;
    [Header("Room Enemy Mix")]
    [Tooltip("Maximum distinct enemy types selected once for the entire room, across all waves.")]
    [SerializeField, Range(1, 5)] private int _maximumEnemyTypesPerRoom = 3;
    [Tooltip("Chance to use just one of the unlocked enemy types. Other rooms mix two or more.")]
    [SerializeField, Range(0f, 100f)] private float _singleEnemyTypeRoomChancePercent = 20f;
    [SerializeField] private EnemySpawnRule[] _enemyRules =
    {
        new(EnemyType.Dummy, 1, 50f, 0),
        new(EnemyType.Bun, 2, 14f, 4),
        new(EnemyType.Bomb, 4, 20f, 8),
        new(EnemyType.Ghost, 6, 18f, 8),
        new(EnemyType.Chan, 8, 5f, 2)
    };

    public EnemySpawnRule[] EnemyRules => _enemyRules;
    public int MaxAliveEnemies => Mathf.Max(1, _maxAliveEnemies);
    public int SpawnBatchSize => Mathf.Max(1, _spawnBatchSize);
    public float SpawnBatchDelay => Mathf.Max(0f, _spawnBatchDelay);
    public int GetStartEnemyCount(int roomIndex) =>
        Mathf.Min(MaxAliveEnemies, GetCount(_startingEnemies, roomIndex, 3));
    public int GetAllEnemyCount(int roomIndex) =>
        Mathf.Max(GetStartEnemyCount(roomIndex), GetCount(_totalEnemies, roomIndex, 3));
    public bool UsesSmallRoom(int roomIndex) =>
        GetAllEnemyCount(roomIndex) <= Mathf.Max(1, _maxEnemiesInSmallRoom);

    public int[] CreateWaveEnemyCounts(int roomIndex)
    {
        int totalEnemies = GetAllEnemyCount(roomIndex);
        float waveChance = Mathf.Clamp01(_waveRoomChancePercent / 100f);
        if (roomIndex < Mathf.Max(1, _firstWaveRoom) - 1 || totalEnemies < 3 ||
            _wavePatterns == null || _wavePatterns.Length == 0 || waveChance <= 0f ||
            (waveChance < 1f && UnityEngine.Random.value >= waveChance))
            return new[] { totalEnemies };

        Vector3Int pattern = _wavePatterns[UnityEngine.Random.Range(0, _wavePatterns.Length)];
        float firstWeight = Mathf.Max(1, pattern.x);
        float middleWeight = Mathf.Max(1, pattern.y);
        float lastWeight = Mathf.Max(1, pattern.z);
        float totalWeight = firstWeight + middleWeight + lastWeight;
        int firstWave = Mathf.Clamp(Mathf.RoundToInt(totalEnemies * firstWeight / totalWeight),
            1, totalEnemies - 2);
        int lastWave = Mathf.Clamp(Mathf.RoundToInt(totalEnemies * lastWeight / totalWeight),
            1, totalEnemies - firstWave - 1);
        return new[] { firstWave, totalEnemies - firstWave - lastWave, lastWave };
    }

    public int GetWaveReinforcementThreshold(int waveEnemyCount)
    {
        int minimum = Mathf.Clamp(_waveRemainingEnemiesRange.x, 1, 2);
        int maximum = Mathf.Clamp(_waveRemainingEnemiesRange.y, minimum, 2);
        int threshold = UnityEngine.Random.Range(minimum, maximum + 1);
        // Even a small wave must lose an enemy before the next wave can start.
        return Mathf.Min(threshold, Mathf.Max(0, waveEnemyCount - 1));
    }

    public bool IsSpecialistRoom(int roomIndex) => roomIndex >= 3 && roomIndex % 3 == 0;
    public bool IsSwarmRoom(int roomIndex) => roomIndex < 2 || roomIndex % 3 == 1;
    public int GetSpecialTypeLimit(int roomIndex) => roomIndex < 3 ? 1 : roomIndex < 6 ? 2 : 3;
    public int RollEnemyTypeCount(int availableTypes, int roomIndex)
    {
        if (availableTypes <= 0)
            return 0;

        int maximum = Mathf.Min(availableTypes, Mathf.Max(1, _maximumEnemyTypesPerRoom),
            GetSpecialTypeLimit(roomIndex) + 1);
        float singleTypeChance = Mathf.Clamp01(_singleEnemyTypeRoomChancePercent / 100f);
        if (maximum == 1 || singleTypeChance >= 1f ||
            (singleTypeChance > 0f && UnityEngine.Random.value < singleTypeChance))
            return 1;

        return UnityEngine.Random.Range(2, maximum + 1);
    }
    public int GetRangedLimit(int roomIndex) => roomIndex < 6 ? 2 : roomIndex < 9 ? 5 : 8;
    public int GetEliteLimit(int roomIndex) =>
        roomIndex + 1 >= _firstEliteRoom &&
        (roomIndex + 1 - _firstEliteRoom) % Mathf.Max(1, _eliteRoomInterval) == 0
            ? (roomIndex < 9 ? 1 : 2)
            : 0;

    public float GetWeight(EnemySpawnRule rule, int roomIndex)
    {
        if (rule.EnemyType == EnemyType.Dummy)
            return rule.Weight;
        return rule.Weight * (IsSpecialistRoom(roomIndex) ? 2.5f : IsSwarmRoom(roomIndex) ? 0.65f : 1f);
    }

    public static bool IsRanged(EnemyType type) => type is EnemyType.Ghost or EnemyType.Chan;

    private static int GetCount(int[] counts, int roomIndex, int fallback)
    {
        if (counts == null || counts.Length == 0)
            return fallback;

        int index = Mathf.Clamp(roomIndex, 0, counts.Length - 1);
        return Mathf.Max(1, counts[index]);
    }
}

[Serializable]
public sealed class EnemySpawnRule
{
    public EnemyType EnemyType;
    [Min(1)] public int FirstRoom = 1;
    [Min(0f)] public float Weight = 1f;
    [Tooltip("Maximum alive at once; zero means unlimited.")]
    [Min(0)] public int MaxAlive;

    public EnemySpawnRule(EnemyType enemyType, int firstRoom, float weight, int maxAlive)
    {
        EnemyType = enemyType;
        FirstRoom = firstRoom;
        Weight = weight;
        MaxAlive = maxAlive;
    }
}
