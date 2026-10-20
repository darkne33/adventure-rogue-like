using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Configs/Levels/Room Catalog", fileName = "LevelRoomCatalog")]
public sealed class LevelRoomCatalog : ScriptableObject
{
    [SerializeField] private Room _startRoom;
    [SerializeField] private Room[] _smallEnemyRooms = Array.Empty<Room>();
    [SerializeField] private Room[] _mediumEnemyRooms = Array.Empty<Room>();
    [SerializeField] private EnemyRoomSettings _enemySettings = new();
    [FormerlySerializedAs("_rewardRooms")]
    [SerializeField] private Room[] _treasureRooms = Array.Empty<Room>();
    [SerializeField] private Room[] _onlyRelicRooms = Array.Empty<Room>();
    [SerializeField] private Room[] _bloodRooms = Array.Empty<Room>();
    [SerializeField] private Room[] _shopRooms = Array.Empty<Room>();
    [Tooltip("Boss room prefabs shuffled each cycle. Every boss appears once before the next cycle.")]
    [SerializeField] private Room[] _bossCycle = Array.Empty<Room>();

    public Room StartRoom => _startRoom;
    public IReadOnlyList<Room> SmallEnemyRooms => _smallEnemyRooms;
    public IReadOnlyList<Room> MediumEnemyRooms => _mediumEnemyRooms;
    public EnemyRoomSettings EnemySettings => _enemySettings;
    public IReadOnlyList<Room> TreasureRooms => _treasureRooms;
    public IReadOnlyList<Room> OnlyRelicRooms => _onlyRelicRooms;
    public IReadOnlyList<Room> BloodRooms => _bloodRooms;
    public IReadOnlyList<Room> ShopRooms => _shopRooms;
    public IReadOnlyList<Room> BossCycle => _bossCycle;

}
