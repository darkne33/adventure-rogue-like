using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Levels/Room Catalog", fileName = "LevelRoomCatalog")]
public sealed class LevelRoomCatalog : ScriptableObject
{
    [SerializeField] private Room _startRoom;
    [SerializeField] private Room[] _smallEnemyRooms = Array.Empty<Room>();
    [SerializeField] private Room[] _mediumEnemyRooms = Array.Empty<Room>();
    [SerializeField] private EnemyRoomSettings _enemySettings = new();
    [SerializeField] private Room[] _rewardRooms = Array.Empty<Room>();
    [SerializeField] private Room[] _shopRooms = Array.Empty<Room>();
    [Tooltip("Boss room prefabs in order. After the last entry the cycle starts again.")]
    [SerializeField] private Room[] _bossCycle = Array.Empty<Room>();

    public Room StartRoom => _startRoom;
    public IReadOnlyList<Room> SmallEnemyRooms => _smallEnemyRooms;
    public IReadOnlyList<Room> MediumEnemyRooms => _mediumEnemyRooms;
    public EnemyRoomSettings EnemySettings => _enemySettings;
    public IReadOnlyList<Room> RewardRooms => _rewardRooms;
    public IReadOnlyList<Room> ShopRooms => _shopRooms;

    public Room GetBossRoom(int levelIndex)
    {
        if (levelIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(levelIndex));
        if (_bossCycle == null || _bossCycle.Length == 0)
            throw new InvalidOperationException($"{name} must contain a boss cycle.");
        Room room = _bossCycle[levelIndex % _bossCycle.Length];
        if (room == null || room.RoomData is not BossRoomData)
            throw new InvalidOperationException($"{name} contains an invalid boss room in its cycle.");
        return room;
    }
}
