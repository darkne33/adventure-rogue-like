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

    public static LevelRoomNode[] Generate(LevelsConfiguration configuration, int levelIndex)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));
        configuration.GetLevel(levelIndex);
        LevelRoomCatalog catalog = configuration.RoomCatalog;
        if (catalog.EnemySettings == null || !catalog.EnemySettings.HasSpawnableEnemies)
            throw new InvalidOperationException("The room catalog must contain spawnable enemy types.");

        ProceduralLevelSettings settings = configuration.ProceduralLevels;
        int combatRooms = settings.GetCombatRoomCount(levelIndex);
        int bossDepth = settings.GetMainPathCombatRoomCount(combatRooms);
        // Roll content once. Retrying geometry must not favour floors with fewer rewards.
        int rewardRooms = settings.RollRewardRoomCount();
        int onlyRelicRooms = settings.OnlyRelicRoomCount;
        bool hasShop = settings.RollShop();
        var templates = new Templates(catalog, levelIndex, rewardRooms > 0,
            onlyRelicRooms > 0, hasShop);

        for (int attempt = 0; attempt < settings.GenerationAttempts; attempt++)
        {
            var layout = new Layout(settings, templates, bossDepth);
            if (!layout.TryBuild(combatRooms - bossDepth, rewardRooms, onlyRelicRooms, hasShop))
                continue;
            return layout.CreateNodes(catalog.EnemySettings);
        }

        throw new InvalidOperationException(
            $"Cannot generate floor {levelIndex + 1} after {settings.GenerationAttempts} attempts " +
            $"({combatRooms} combat, {rewardRooms} chest reward, {onlyRelicRooms} relic, " +
            $"{(hasShop ? 1 : 0)} shop rooms). " +
            "Check the catalog's door compatibility, grid radius and branch constraints.");
    }

    private sealed class Templates
    {
        private readonly Dictionary<RoomType, List<Room>> _pools = new();
        private readonly Dictionary<(RoomType, RoomConnectionMask, RoomDirection?), List<Room>> _compatible = new();

        public Templates(LevelRoomCatalog catalog, int levelIndex, bool needsRewards,
            bool needsRelics, bool needsShop)
        {
            AddPool(RoomType.Start, new[] { catalog.StartRoom });
            AddPool(RoomType.Enemy, catalog.SmallEnemyRooms);
            AddPool(RoomType.Enemy, catalog.MediumEnemyRooms);
            AddPool(RoomType.Boss, new[] { catalog.GetBossRoom(levelIndex) });
            if (needsRewards)
                AddPool(RoomType.Reward, catalog.RewardRooms);
            if (needsRelics)
                AddPool(RoomType.OnlyRelic, catalog.OnlyRelicRooms);
            if (needsShop)
                AddPool(RoomType.Shop, catalog.ShopRooms);

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
                if (type == RoomType.Start && room.RoomData is not StartRoomData)
                    throw new InvalidOperationException($"{room.name} must contain StartRoomData.");
                if (type == RoomType.Enemy &&
                    (room.RoomData is not DefaultEnemiesRoomData || room.RoomData is BossRoomData))
                    throw new InvalidOperationException($"{room.name} must be an ordinary combat room.");
                if (type == RoomType.OnlyRelic && room.RoomData is not OnlyRelicRoomData)
                    throw new InvalidOperationException($"{room.name} must contain OnlyRelicRoomData.");
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
        private readonly int _bossDepth;
        private readonly List<LayoutRoom> _rooms = new();
        private readonly HashSet<Vector2Int> _occupied = new();
        private Vector2Int? _reservedExit;

        public Layout(ProceduralLevelSettings settings, Templates templates, int bossDepth)
        {
            _settings = settings;
            _templates = templates;
            _bossDepth = bossDepth;
            _rooms.Add(new LayoutRoom(Vector2Int.zero, RoomType.Start, 0, 0, true));
            _occupied.Add(Vector2Int.zero);
        }

        public bool TryBuild(int sideCombatRooms, int rewardRooms, int onlyRelicRooms, bool hasShop)
        {
            // A self-avoiding route may turn in any of the four directions.
            // Side branches are subsequently restricted to keep the boss a farthest leaf.
            for (int depth = 1; depth <= _bossDepth; depth++)
            {
                LayoutRoom parent = _rooms[_rooms.Count - 1];
                RoomType type = depth == _bossDepth ? RoomType.Boss : RoomType.Enemy;
                var options = new List<RoomDirection>(Directions);
                Shuffle(options);
                bool added = false;
                foreach (RoomDirection direction in options)
                {
                    if (!CanPlace(parent, direction, type))
                        continue;
                    Add(parent, direction, type, onMainPath: true);
                    added = true;
                    break;
                }
                if (!added)
                    return false;
            }

            for (int i = 0; i < sideCombatRooms; i++)
                if (!TryAddBranch(RoomType.Enemy, requireEarlyFork: i == 0))
                    return false;

            // Prefer rewards at the ends of optional combat branches.
            for (int i = 0; i < rewardRooms; i++)
                if (!TryAddBranch(RoomType.Reward))
                    return false;
            for (int i = 0; i < onlyRelicRooms; i++)
                if (!TryAddBranch(RoomType.OnlyRelic))
                    return false;
            return !hasShop || TryAddBranch(RoomType.Shop);
        }

        private bool TryAddBranch(RoomType type, bool requireEarlyFork = false)
        {
            var candidates = new List<Attachment>();
            foreach (LayoutRoom parent in _rooms)
            {
                if (parent.Type != RoomType.Enemy || parent.ConnectionCount >= 3)
                    continue;
                if (type == RoomType.Enemy)
                {
                    if (parent.Depth + 1 >= _bossDepth ||
                        parent.BranchDepth >= _settings.MaximumBranchCombatRooms)
                        continue;
                    if (!parent.OnMainPath && parent.ConnectionCount != 1)
                        continue;
                    if (requireEarlyFork &&
                        (!parent.OnMainPath || parent.Depth > _settings.FirstBranchMaximumDepth))
                        continue;
                }
                else if (parent.Depth + 1 > _bossDepth || parent.HasSpecialChild)
                {
                    continue;
                }

                foreach (RoomDirection direction in Directions)
                {
                    if (!CanPlace(parent, direction, type))
                        continue;
                    int priority = type is RoomType.Reward or RoomType.OnlyRelic
                        ? (parent.ConnectionCount == 1 ? 2 : 0) + (!parent.OnMainPath ? 1 : 0)
                        : 0;
                    candidates.Add(new Attachment(parent, direction, priority));
                }
            }

            if (candidates.Count == 0)
                return false;
            Shuffle(candidates);
            int bestPriority = 0;
            foreach (Attachment candidate in candidates)
                bestPriority = Math.Max(bestPriority, candidate.Priority);
            foreach (Attachment candidate in candidates)
            {
                if (candidate.Priority != bestPriority)
                    continue;
                Add(candidate.Parent, candidate.Direction, type, onMainPath: false);
                return true;
            }
            return false;
        }

        private bool CanPlace(LayoutRoom parent, RoomDirection direction, RoomType type)
        {
            Vector2Int position = parent.Position + direction.ToGridOffset();
            if (Mathf.Abs(position.x) > _settings.GridRadius ||
                Mathf.Abs(position.y) > _settings.GridRadius ||
                _occupied.Contains(position) || position == _reservedExit)
                return false;

            // Leave air between unrelated corridors; grid adjacency cannot create shortcuts.
            foreach (RoomDirection neighborDirection in Directions)
            {
                Vector2Int neighbor = position + neighborDirection.ToGridOffset();
                if (neighbor != parent.Position && _occupied.Contains(neighbor))
                    return false;
            }

            RoomConnectionMask childConnections = direction.Opposite().ToConnectionMask();
            RoomDirection? bossExit = null;
            if (type == RoomType.Boss)
            {
                Vector2Int exitPosition = position + direction.ToGridOffset();
                if (_occupied.Contains(exitPosition))
                    return false;
                bossExit = direction;
                childConnections |= direction.ToConnectionMask();
            }

            return _templates.Get(parent.Type, parent.Connections | direction.ToConnectionMask()).Count > 0 &&
                   _templates.Get(type, childConnections, bossExit).Count > 0;
        }

        private void Add(LayoutRoom parent, RoomDirection direction, RoomType type, bool onMainPath)
        {
            var room = new LayoutRoom(parent.Position + direction.ToGridOffset(), type,
                parent.Depth + 1, onMainPath ? 0 : parent.BranchDepth + 1, onMainPath);
            parent.Connections |= direction.ToConnectionMask();
            room.Connections = direction.Opposite().ToConnectionMask();
            if (type == RoomType.Boss)
            {
                room.ExitDirection = direction;
                room.Connections |= direction.ToConnectionMask();
                _reservedExit = room.Position + direction.ToGridOffset();
            }
            if (type is RoomType.Reward or RoomType.OnlyRelic or RoomType.Shop)
                parent.HasSpecialChild = true;
            _rooms.Add(room);
            _occupied.Add(room.Position);
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

    private readonly struct Attachment
    {
        public readonly LayoutRoom Parent;
        public readonly RoomDirection Direction;
        public readonly int Priority;

        public Attachment(LayoutRoom parent, RoomDirection direction, int priority)
        {
            Parent = parent;
            Direction = direction;
            Priority = priority;
        }
    }

    private sealed class LayoutRoom
    {
        public readonly Vector2Int Position;
        public readonly RoomType Type;
        public readonly int Depth;
        public readonly int BranchDepth;
        public readonly bool OnMainPath;
        public RoomConnectionMask Connections;
        public RoomDirection ExitDirection;
        public bool HasSpecialChild;

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

        public LayoutRoom(Vector2Int position, RoomType type, int depth, int branchDepth, bool onMainPath)
        {
            Position = position;
            Type = type;
            Depth = depth;
            BranchDepth = branchDepth;
            OnMainPath = onMainPath;
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
