using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public static class ProceduralLevelGenerator
{
    private static readonly RoomDirection[] Directions =
    {
        RoomDirection.Up, RoomDirection.Down, RoomDirection.Left, RoomDirection.Right
    };

    public static LevelRoomNode[] Generate(LevelsConfiguration configuration, int levelIndex,
        int coins = 0, int keys = 0)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));
        configuration.GetLevel(levelIndex);
        LevelRoomCatalog catalog = configuration.RoomCatalog;
        if (catalog.EnemySettings == null || !catalog.EnemySettings.HasSpawnableEnemies)
            throw new InvalidOperationException("The room catalog must contain spawnable enemy types.");

        ProceduralLevelSettings settings = configuration.ProceduralLevels;
        // Roll content once. Geometry retries must not change floor size or room chances.
        int roomCount = settings.RollRoomCount(levelIndex);
        List<RoomType> specialRooms = settings.RollSpecialRooms(levelIndex, coins, keys);
        var templates = new Templates(catalog, levelIndex, specialRooms);

        for (int attempt = 0; attempt < settings.GenerationAttempts; attempt++)
        {
            var layout = new Layout(settings, templates);
            if (layout.TryBuild(roomCount, settings.GetMinimumDeadEnds(levelIndex), specialRooms))
                return layout.CreateNodes(catalog.EnemySettings);
        }

        throw new InvalidOperationException(
            $"Cannot generate floor {levelIndex + 1} after {settings.GenerationAttempts} attempts " +
            $"({roomCount} total rooms, special rooms: {string.Join(", ", specialRooms)}). " +
            "Check the catalog's door compatibility and available dead ends.");
    }

    private sealed class Templates
    {
        private readonly Dictionary<RoomType, List<Room>> _pools = new();
        private readonly Dictionary<(RoomType, RoomConnectionMask, RoomDirection?), List<Room>> _compatible = new();

        public Templates(LevelRoomCatalog catalog, int levelIndex, IReadOnlyList<RoomType> specialRooms)
        {
            AddPool(RoomType.Start, new[] { catalog.StartRoom });
            AddPool(RoomType.Enemy, catalog.SmallEnemyRooms);
            AddPool(RoomType.Enemy, catalog.MediumEnemyRooms);
            AddPool(RoomType.Boss, new[] { catalog.GetBossRoom(levelIndex) });
            foreach (RoomType type in specialRooms)
            {
                IReadOnlyList<Room> rooms = type switch
                {
                    RoomType.Reward => catalog.RewardRooms,
                    RoomType.OnlyRelic => catalog.OnlyRelicRooms,
                    RoomType.Blood => catalog.BloodRooms,
                    RoomType.Shop => catalog.ShopRooms,
                    _ => throw new InvalidOperationException($"Unsupported special room type: {type}.")
                };
                AddPool(type, rooms);
            }

            foreach (KeyValuePair<RoomType, List<Room>> pool in _pools)
                if (pool.Value.Count == 0)
                    throw new InvalidOperationException($"The room catalog has no {pool.Key} prefabs.");
        }

        private void AddPool(RoomType type, IReadOnlyList<Room> rooms)
        {
            if (!_pools.TryGetValue(type, out List<Room> pool))
            {
                pool = new List<Room>();
                _pools.Add(type, pool);
            }
            if (rooms == null)
                return;
            foreach (Room room in rooms)
            {
                if (room == null)
                    throw new InvalidOperationException($"The {type} room catalog contains a missing prefab.");
                if (room.transform.parent != null)
                    throw new InvalidOperationException($"{room.name} must be a standalone room prefab.");
                bool matches = type switch
                {
                    RoomType.Start => room.RoomData is StartRoomData,
                    RoomType.Enemy => room.RoomData is DefaultEnemiesRoomData and not BossRoomData,
                    RoomType.Boss => room.RoomData is BossRoomData,
                    RoomType.Reward => room.RoomData is RewardRoomData,
                    RoomType.OnlyRelic => room.RoomData is OnlyRelicRoomData,
                    RoomType.Blood => room.RoomData is BloodRoomData,
                    RoomType.Shop => room.RoomData is ShopRoomData,
                    _ => false
                };
                if (!matches)
                    throw new InvalidOperationException($"{room.name} has invalid data for a {type} room.");
                if (!pool.Contains(room))
                    pool.Add(room);
            }
        }

        public List<Room> Get(RoomType type, RoomConnectionMask connections, RoomDirection? bossExit = null)
        {
            var key = (type, connections, bossExit);
            if (_compatible.TryGetValue(key, out List<Room> cached))
                return cached;

            var requiredDirections = new List<RoomDirection>();
            foreach (RoomDirection direction in Directions)
                if ((connections & direction.ToConnectionMask()) != RoomConnectionMask.None)
                    requiredDirections.Add(direction);
            var result = new List<Room>();
            if (_pools.TryGetValue(type, out List<Room> pool))
                foreach (Room room in pool)
                    if (LevelView.CanFitRoom(room, requiredDirections, bossExit))
                        result.Add(room);
            _compatible.Add(key, result);
            return result;
        }
    }

    private sealed class Layout
    {
        private readonly ProceduralLevelSettings _settings;
        private readonly Templates _templates;
        private readonly List<LayoutRoom> _rooms = new();
        private readonly HashSet<Vector2Int> _occupied = new();

        public Layout(ProceduralLevelSettings settings, Templates templates)
        {
            _settings = settings;
            _templates = templates;
            _rooms.Add(new LayoutRoom(Vector2Int.zero, RoomType.Start, 0));
            _occupied.Add(Vector2Int.zero);
        }

        public bool TryBuild(int roomCount, int minimumDeadEnds, IReadOnlyList<RoomType> specialRooms)
        {
            // The growing list is a breadth-first queue. A candidate cell has one
            // occupied neighbour and a 50% admission roll, so corridors form a tree.
            for (int index = 0; index < _rooms.Count && _rooms.Count < roomCount; index++)
            {
                Grow(_rooms[index], roomCount);
                if (roomCount > 16 && index % 4 == 0 && _rooms.Count < roomCount)
                    Grow(_rooms[0], roomCount);
            }
            if (_rooms.Count != roomCount)
                return false;

            var deadEnds = _rooms.FindAll(room => room.Type != RoomType.Start && room.ConnectionCount == 1);
            if (deadEnds.Count < minimumDeadEnds)
                return false;
            Shuffle(deadEnds);
            deadEnds.Sort((left, right) => right.Depth.CompareTo(left.Depth));

            if (!AssignBoss(deadEnds))
                return false;

            // Convert existing dead ends instead of adding compulsory extra branches.
            // The supported special types follow Rebirth's placement order.
            foreach (RoomType type in specialRooms)
            {
                if (deadEnds.Count == 0)
                    break;
                LayoutRoom room = deadEnds[0];
                if (_templates.Get(type, room.Connections).Count == 0)
                    return false;
                room.Type = type;
                deadEnds.RemoveAt(0);
            }
            return true;
        }

        private void Grow(LayoutRoom parent, int roomCount)
        {
            foreach (RoomDirection direction in Directions)
            {
                if (_rooms.Count >= roomCount)
                    return;
                Vector2Int position = parent.Position + direction.ToGridOffset();
                if (Mathf.Abs(position.x) > _settings.GridRadius ||
                    Mathf.Abs(position.y) > _settings.GridRadius || _occupied.Contains(position))
                    continue;

                int neighbors = 0;
                foreach (RoomDirection neighborDirection in Directions)
                    if (_occupied.Contains(position + neighborDirection.ToGridOffset()))
                        neighbors++;
                if (neighbors != 1)
                    continue;

                RoomConnectionMask parentConnections = parent.Connections | direction.ToConnectionMask();
                RoomConnectionMask childConnections = direction.Opposite().ToConnectionMask();
                if (_templates.Get(parent.Type, parentConnections).Count == 0 ||
                    _templates.Get(RoomType.Enemy, childConnections).Count == 0 || Random.value >= 0.5f)
                    continue;

                parent.Connections = parentConnections;
                _rooms.Add(new LayoutRoom(position, RoomType.Enemy, parent.Depth + 1)
                {
                    Connections = childConnections
                });
                _occupied.Add(position);
            }
        }

        private bool AssignBoss(List<LayoutRoom> deadEnds)
        {
            int maximumDepth = deadEnds[0].Depth;
            if (maximumDepth < 2)
                return false;

            // Boss prefabs need both an entrance and their authored floor-exit door.
            // Try all equally distant leaves before discarding the geometry.
            for (int index = 0; index < deadEnds.Count; index++)
            {
                LayoutRoom room = deadEnds[index];
                if (room.Depth != maximumDepth)
                    break;
                RoomDirection entrance = GetEntranceDirection(room);
                RoomDirection exit = entrance.Opposite();
                if (_occupied.Contains(room.Position + exit.ToGridOffset()))
                    continue;
                RoomConnectionMask connections = room.Connections | exit.ToConnectionMask();
                if (_templates.Get(RoomType.Boss, connections, exit).Count == 0)
                    continue;

                room.Type = RoomType.Boss;
                room.ExitDirection = exit;
                room.Connections = connections;
                deadEnds.RemoveAt(index);
                return true;
            }
            return false;
        }

        private static RoomDirection GetEntranceDirection(LayoutRoom room)
        {
            foreach (RoomDirection direction in Directions)
                if ((room.Connections & direction.ToConnectionMask()) != RoomConnectionMask.None)
                    return direction;
            throw new InvalidOperationException("A dead-end room must have one entrance.");
        }

        public LevelRoomNode[] CreateNodes(EnemyRoomSettings enemySettings)
        {
            var nodes = new LevelRoomNode[_rooms.Count];
            var used = new HashSet<Room>();
            for (int i = 0; i < _rooms.Count; i++)
            {
                LayoutRoom room = _rooms[i];
                List<Room> compatible = _templates.Get(room.Type, room.Connections,
                    room.Type == RoomType.Boss ? room.ExitDirection : null);
                var available = compatible.FindAll(prefab => !used.Contains(prefab));
                if (available.Count == 0)
                {
                    foreach (Room prefab in compatible)
                        used.Remove(prefab);
                    available = compatible;
                }
                Room selected = available[Random.Range(0, available.Count)];
                used.Add(selected);
                nodes[i] = new LevelRoomNode(selected, room.Position, room.Type,
                    room.ExitDirection, room.Type == RoomType.Enemy ? enemySettings : null,
                    RoomConnectionMask.All & ~room.Connections);
            }
            return nodes;
        }
    }

    private sealed class LayoutRoom
    {
        public readonly Vector2Int Position;
        public readonly int Depth;
        public RoomType Type;
        public RoomConnectionMask Connections;
        public RoomDirection ExitDirection;

        public int ConnectionCount
        {
            get
            {
                int count = 0;
                foreach (RoomDirection direction in Directions)
                    if ((Connections & direction.ToConnectionMask()) != RoomConnectionMask.None)
                        count++;
                return count;
            }
        }

        public LayoutRoom(Vector2Int position, RoomType type, int depth)
        {
            Position = position;
            Type = type;
            Depth = depth;
        }
    }

    private static void Shuffle<T>(List<T> values)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int other = Random.Range(0, i + 1);
            (values[i], values[other]) = (values[other], values[i]);
        }
    }
}
