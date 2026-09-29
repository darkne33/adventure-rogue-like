using System;
using UnityEngine;

[Serializable]
public sealed class OpeningLevelGenerationSettings
{
    [Tooltip("Vary the number of ordinary enemy rooms around each of the first three examples.")]
    [SerializeField, Range(0, 3)] private int _enemyRoomVariation = 1;
    [Tooltip("Share of ordinary enemy rooms on the route to the boss. The rest form side branches.")]
    [SerializeField, Range(0.5f, 0.9f)] private float _mainPathEnemyFraction = 0.7f;

    [Header("Optional Shop")]
    [Tooltip("Room counts exclude the optional shop and include Start, rewards, and the final room.")]
    [SerializeField, Min(1)] private int _smallLevelRoomCount = 6;
    [SerializeField, Min(2)] private int _largeLevelRoomCount = 18;
    [SerializeField, Range(0f, 100f)] private float _smallLevelShopChancePercent = 20f;
    [SerializeField, Range(0f, 100f)] private float _largeLevelShopChancePercent = 80f;

    public int EnemyRoomVariation => Mathf.Clamp(_enemyRoomVariation, 0, 3);

    public int GetMainPathEnemyRoomCount(int enemyRooms) =>
        Mathf.Clamp(Mathf.RoundToInt(enemyRooms * Mathf.Clamp(_mainPathEnemyFraction, 0.5f, 0.9f)),
            2, Mathf.Max(2, enemyRooms - 1));

    public float GetShopChance(int roomCount)
    {
        int small = Mathf.Max(1, _smallLevelRoomCount);
        int large = Mathf.Max(small + 1, _largeLevelRoomCount);
        float minimum = Mathf.Clamp01(_smallLevelShopChancePercent / 100f);
        float maximum = Mathf.Max(minimum, Mathf.Clamp01(_largeLevelShopChancePercent / 100f));
        return Mathf.Lerp(minimum, maximum, Mathf.InverseLerp(small, large, roomCount));
    }

    public int GetDefaultCombatRoomsToExit(LevelView example)
    {
        int enemyRooms = 0;
        foreach (LevelRoomNode node in example.Rooms)
            if (node != null && node.Type == RoomType.Enemy)
                enemyRooms++;

        return GetMainPathEnemyRoomCount(Mathf.Max(3, enemyRooms)) + 1;
    }
}
