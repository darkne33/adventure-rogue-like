using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Enemies/Enemy Room Scaling Configuration",
    fileName = "EnemyRoomScalingConfiguration", order = 0)]
public class EnemyRoomScalingConfiguration : ScriptableObject
{
    [Tooltip("One entry per combat depth. Later rooms repeat the final three encounters.")]
    [SerializeField] private int[] _startingEnemies = { 3, 5, 8, 7, 12, 16, 12, 22, 28, 20, 36, 40 };
    [SerializeField] private int[] _totalEnemies = { 3, 5, 10, 10, 18, 26, 22, 38, 50, 40, 68, 80 };
    [Tooltip("Maximum simultaneous enemies for a Small room. Larger groups use Medium rooms.")]
    [SerializeField, Min(1)] private int _maxEnemiesInSmallRoom = 8;
    [SerializeField, Range(0f, 0.5f)] private float _reinforcementRemainingFraction = 0.25f;
    [SerializeField, Min(1)] private int _firstEliteRoom = 4;
    [SerializeField, Min(1)] private int _eliteRoomInterval = 3;
    [SerializeField] private EnemySpawnRule[] _enemyRules =
    {
        new(EnemyType.Dummy, 1, 70f, 0),
        new(EnemyType.Bun, 2, 14f, 4),
        new(EnemyType.Bomb, 4, 10f, 3),
        new(EnemyType.Ghost, 6, 8f, 3),
        new(EnemyType.Chan, 8, 5f, 2)
    };

    public EnemySpawnRule[] EnemyRules => _enemyRules;
    public int GetStartEnemyCount(int roomIndex) => GetCount(_startingEnemies, roomIndex, 3);
    public int GetAllEnemyCount(int roomIndex) =>
        Mathf.Max(GetStartEnemyCount(roomIndex), GetCount(_totalEnemies, roomIndex, 3));
    public bool UsesSmallRoom(int roomIndex) =>
        GetStartEnemyCount(roomIndex) <= Mathf.Max(1, _maxEnemiesInSmallRoom);

    public bool IsSpecialistRoom(int roomIndex) => roomIndex >= 3 && roomIndex % 3 == 0;
    public bool IsSwarmRoom(int roomIndex) => roomIndex < 2 || roomIndex % 3 == 1;
    public int GetSpecialTypeLimit(int roomIndex) => roomIndex < 3 ? 1 : roomIndex < 6 ? 2 : 3;
    public int GetRangedLimit(int roomIndex) => roomIndex < 6 ? 2 : roomIndex < 9 ? 3 : 4;
    public int GetReinforcementThreshold(int roomIndex) => IsSpecialistRoom(roomIndex)
        ? 0
        : Mathf.FloorToInt(GetStartEnemyCount(roomIndex) * _reinforcementRemainingFraction);

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

        int index = Mathf.Max(0, roomIndex);
        if (index >= counts.Length)
        {
            int cycleLength = Mathf.Min(3, counts.Length);
            index = counts.Length - cycleLength + (index - counts.Length) % cycleLength;
        }

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
