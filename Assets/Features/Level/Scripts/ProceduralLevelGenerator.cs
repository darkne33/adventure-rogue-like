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

    public static LevelRoomNode[] GenerateFromExample(LevelsConfiguration configuration, int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= LevelsConfiguration.AuthoredLevelCount)
            throw new ArgumentOutOfRangeException(nameof(levelIndex));

        LevelView example = configuration.GetLevel(levelIndex).LevelView;
        OpeningLevelGenerationSettings settings = configuration.OpeningLevels;
        Dictionary<RoomType, List<LevelRoomNode>> templates = CollectExampleTemplates(example);
        int finalRooms = GetTemplateCount(templates, RoomType.Boss) + GetTemplateCount(templates, RoomType.Exit);
        if (GetTemplateCount(templates, RoomType.Start) != 1 || finalRooms != 1 ||
            GetTemplateCount(templates, RoomType.Enemy) == 0)
            throw new InvalidOperationException(
                $"{example.name} must provide one Start, one Boss or Exit, and ordinary enemy room examples.");

        int variation = settings.EnemyRoomVariation;
        int enemyRooms = Mathf.Max(3, GetTemplateCount(templates, RoomType.Enemy) +
                                      Random.Range(-variation, variation + 1));
        int mainPathEnemies = settings.GetMainPathEnemyRoomCount(enemyRooms);
        int rewardRooms = GetTemplateCount(templates, RoomType.Reward);
        int roomsWithoutShop = 2 + enemyRooms + rewardRooms;
        bool hasShop = templates.ContainsKey(RoomType.Shop) &&
                       Random.value < settings.GetShopChance(roomsWithoutShop);
        RoomType finalType = templates.ContainsKey(RoomType.Boss) ? RoomType.Boss : RoomType.Exit;

        // Only geometry is retried. Counts and the shop roll stay fixed for this level.
        // Every node refers to an already loaded prefab from this specific example.
        for (int attempt = 0; attempt < 32; attempt++)
        {
            if (!TryCreateOpeningPath(mainPathEnemies, finalType, templates,
                    out List<LayoutRoom> layout, out HashSet<Vector2Int> occupied,
                    out Vector2Int exitPosition))
                continue;

            bool complete = true;
            for (int i = mainPathEnemies; i < enemyRooms && complete; i++)
                complete = TryAddBranch(layout, occupied, exitPosition, RoomType.Enemy,
                    templates, mainPathEnemies - 1);

            var specialRooms = new List<RoomType>();
            for (int i = 0; i < rewardRooms; i++)
                specialRooms.Add(RoomType.Reward);
            if (hasShop)
                specialRooms.Add(RoomType.Shop);
            Shuffle(specialRooms);
            foreach (RoomType type in specialRooms)
            {
                if (!complete)
                    break;
                complete = TryAddBranch(layout, occupied, exitPosition, type, templates, mainPathEnemies);
            }

            if (complete)
                return CreateNodes(layout, templates);
        }

        throw new InvalidOperationException(
            $"Cannot generate a connected layout from {example.name}. " +
            "Its room prefabs must support the main path and side branches.");
    }

    public static LevelRoomNode[] Generate(LevelsConfiguration configuration, int levelIndex)
    {
        ProceduralLevelSettings settings = configuration.ProceduralLevels;
        int combatRooms = settings.GetCombatRoomCount(levelIndex - LevelsConfiguration.AuthoredLevelCount);
        Dictionary<RoomType, List<LevelRoomNode>> templates = CollectTemplates(configuration);
        RoomType finalType = templates.ContainsKey(RoomType.Boss) ? RoomType.Boss : RoomType.Exit;
        var layout = new List<LayoutRoom>();
        var occupied = new HashSet<Vector2Int>();

        RoomDirection forward = Directions[Random.Range(0, Directions.Length)];
        RoomDirection sideways = forward.RotateClockwise(Random.value < 0.5f ? 1 : -1);
        var start = new LayoutRoom(Vector2Int.zero, RoomType.Start);
        layout.Add(start);
        occupied.Add(start.Position);

        // Each step advances on one of two perpendicular axes. The route cannot
        // overlap itself, get trapped, or introduce a shortcut to the final room.
        RoomDirection lastDirection = forward;
        for (int i = 0; i < combatRooms; i++)
        {
            lastDirection = Random.value < 0.5f ? forward : sideways;
            LayoutRoom previous = layout[layout.Count - 1];
            var next = new LayoutRoom(previous.Position + lastDirection.ToGridOffset(),
                i == combatRooms - 1 ? finalType : RoomType.Enemy);
            Connect(previous, next, lastDirection);
            layout.Add(next);
            occupied.Add(next.Position);
        }

        LayoutRoom finalRoom = layout[layout.Count - 1];
        finalRoom.ExitDirection = lastDirection;
        finalRoom.Connections |= lastDirection.ToConnectionMask();
        Vector2Int exitPosition = finalRoom.Position + lastDirection.ToGridOffset();

        // Special rooms are leaves. Explicit connection masks keep nearby
        // branches from accidentally opening into each other or bypassing fights.
        var branches = new List<RoomType>();
        for (int i = 0; i < settings.RewardRooms; i++)
            branches.Add(RoomType.Reward);
        for (int i = 0; i < settings.ShopRooms; i++)
            branches.Add(RoomType.Shop);
        Shuffle(branches);
        foreach (RoomType type in branches)
            AddBranch(layout, occupied, exitPosition, type, templates);

        return CreateNodes(layout, templates);
    }

    private static LevelRoomNode[] CreateNodes(List<LayoutRoom> layout,
        Dictionary<RoomType, List<LevelRoomNode>> templates)
    {
        var nodes = new LevelRoomNode[layout.Count];
        for (int i = 0; i < layout.Count; i++)
        {
            LayoutRoom room = layout[i];
            List<LevelRoomNode> compatible = GetCompatibleTemplates(templates, room.Type, room.Connections,
                room.Type == RoomType.Boss ? room.ExitDirection : null);
            if (compatible.Count == 0)
                throw new InvalidOperationException(
                    $"No {room.Type} prefab fits generated room {room.Position} ({room.Connections}).");

            LevelRoomNode source = compatible[Random.Range(0, compatible.Count)];
            nodes[i] = new LevelRoomNode(source.RoomPrefab, room.Position, room.Type,
                room.ExitDirection, source.EnemySettings, RoomConnectionMask.All & ~room.Connections);
        }

        return nodes;
    }

    private static Dictionary<RoomType, List<LevelRoomNode>> CollectExampleTemplates(LevelView example)
    {
        var result = new Dictionary<RoomType, List<LevelRoomNode>>();
        foreach (LevelRoomNode node in example.Rooms)
        {
            if (node?.RoomPrefab == null)
                throw new InvalidOperationException($"{example.name} contains a missing room prefab.");
            if (node.RoomPrefab.transform.IsChildOf(example.transform))
                throw new InvalidOperationException(
                    $"Opening room {node.RoomPrefab.name} must reference a standalone room prefab.");
            if (node.Type is RoomType.Enemy or RoomType.Exit &&
                (node.EnemySettings == null || !node.EnemySettings.HasSpawnableEnemies))
                throw new InvalidOperationException(
                    $"{example.name} contains a combat room without spawnable enemies.");

            if (!result.TryGetValue(node.Type, out List<LevelRoomNode> pool))
            {
                pool = new List<LevelRoomNode>();
                result.Add(node.Type, pool);
            }

            // Keep repeated entries: their count and encounter settings define this level's profile.
            pool.Add(node);
        }
        return result;
    }

    private static int GetTemplateCount(Dictionary<RoomType, List<LevelRoomNode>> templates, RoomType type) =>
        templates.TryGetValue(type, out List<LevelRoomNode> pool) ? pool.Count : 0;

    private static bool TryCreateOpeningPath(int enemyRooms, RoomType finalType,
        Dictionary<RoomType, List<LevelRoomNode>> templates, out List<LayoutRoom> layout,
        out HashSet<Vector2Int> occupied, out Vector2Int exitPosition)
    {
        var start = new LayoutRoom(Vector2Int.zero, RoomType.Start);
        layout = new List<LayoutRoom> { start };
        occupied = new HashSet<Vector2Int> { start.Position };
        exitPosition = default;
        RoomDirection forward = Directions[Random.Range(0, Directions.Length)];
        RoomDirection sideways = forward.RotateClockwise(Random.value < 0.5f ? 1 : -1);
        var options = new List<RoomDirection> { forward, sideways };

        // A monotone path cannot intersect itself. Side branches supply the extra
        // fights, while explicit masks prevent adjacent cells from creating shortcuts.
        for (int i = 0; i <= enemyRooms; i++)
        {
            LayoutRoom parent = layout[layout.Count - 1];
            bool isFinal = i == enemyRooms;
            RoomType type = isFinal ? finalType : RoomType.Enemy;
            Shuffle(options);
            bool added = false;
            foreach (RoomDirection direction in options)
            {
                RoomConnectionMask connections = direction.Opposite().ToConnectionMask();
                if (isFinal)
                    connections |= direction.ToConnectionMask();
                if (GetCompatibleTemplates(templates, parent.Type,
                        parent.Connections | direction.ToConnectionMask()).Count == 0 ||
                    GetCompatibleTemplates(templates, type, connections,
                        type == RoomType.Boss ? direction : null).Count == 0)
                    continue;

                var room = new LayoutRoom(parent.Position + direction.ToGridOffset(), type);
                Connect(parent, room, direction);
                layout.Add(room);
                occupied.Add(room.Position);
                if (isFinal)
                {
                    room.ExitDirection = direction;
                    room.Connections |= direction.ToConnectionMask();
                    exitPosition = room.Position + direction.ToGridOffset();
                }
                added = true;
                break;
            }

            if (!added)
                return false;
        }

        return true;
    }

    private static Dictionary<RoomType, List<LevelRoomNode>> CollectTemplates(LevelsConfiguration configuration)
    {
        var result = new Dictionary<RoomType, List<LevelRoomNode>>();
        // These prefabs are serialized dependencies of the already loaded level
        // configuration; their Addressables lifetime belongs to the game scene.
        for (int i = 0; i < LevelsConfiguration.AuthoredLevelCount; i++)
        {
            LevelView level = configuration.GetLevel(i).LevelView;
            foreach (LevelRoomNode node in level.Rooms)
            {
                if (node?.RoomPrefab == null)
                    continue;
                if (node.RoomPrefab.transform.IsChildOf(level.transform))
                    throw new InvalidOperationException(
                        $"Procedural room {node.RoomPrefab.name} must reference a standalone room prefab.");
                if (node.Type is RoomType.Enemy or RoomType.Exit &&
                    (node.EnemySettings == null || !node.EnemySettings.HasSpawnableEnemies))
                    continue;

                if (!result.TryGetValue(node.Type, out List<LevelRoomNode> pool))
                {
                    pool = new List<LevelRoomNode>();
                    result.Add(node.Type, pool);
                }

                if (!pool.Exists(existing => existing.RoomPrefab == node.RoomPrefab))
                    pool.Add(node);
            }
        }
        return result;
    }

    private static void AddBranch(List<LayoutRoom> layout, HashSet<Vector2Int> occupied,
        Vector2Int exitPosition, RoomType type, Dictionary<RoomType, List<LevelRoomNode>> templates)
    {
        if (!TryAddBranch(layout, occupied, exitPosition, type, templates))
            throw new InvalidOperationException(
                $"Cannot attach a generated {type} room. Reduce branch counts or add compatible room prefabs.");
    }

    private static bool TryAddBranch(List<LayoutRoom> layout, HashSet<Vector2Int> occupied,
        Vector2Int exitPosition, RoomType type, Dictionary<RoomType, List<LevelRoomNode>> templates,
        int maximumDepth = int.MaxValue)
    {
        var candidates = new List<(LayoutRoom Parent, RoomDirection Direction)>();
        foreach (LayoutRoom parent in layout)
        {
            if (parent.Type != RoomType.Enemy || parent.Depth >= maximumDepth)
                continue;

            foreach (RoomDirection direction in Directions)
            {
                Vector2Int position = parent.Position + direction.ToGridOffset();
                if (occupied.Contains(position) || position == exitPosition)
                    continue;
                if (GetCompatibleTemplates(templates, parent.Type,
                        parent.Connections | direction.ToConnectionMask()).Count == 0 ||
                    GetCompatibleTemplates(templates, type, direction.Opposite().ToConnectionMask()).Count == 0)
                    continue;

                candidates.Add((parent, direction));
            }
        }

        if (candidates.Count == 0)
            return false;

        var selected = candidates[Random.Range(0, candidates.Count)];
        var branch = new LayoutRoom(selected.Parent.Position + selected.Direction.ToGridOffset(), type);
        Connect(selected.Parent, branch, selected.Direction);
        layout.Add(branch);
        occupied.Add(branch.Position);
        return true;
    }

    private static List<LevelRoomNode> GetCompatibleTemplates(
        Dictionary<RoomType, List<LevelRoomNode>> templates, RoomType type, RoomConnectionMask connections,
        RoomDirection? bossDoorDirection = null)
    {
        var result = new List<LevelRoomNode>();
        if (!templates.TryGetValue(type, out List<LevelRoomNode> pool))
            return result;

        var required = new List<RoomDirection>();
        foreach (RoomDirection direction in Directions)
            if ((connections & direction.ToConnectionMask()) != RoomConnectionMask.None)
                required.Add(direction);
        foreach (LevelRoomNode node in pool)
            if (LevelView.CanFitRoom(node.RoomPrefab, required, bossDoorDirection))
                result.Add(node);
        return result;
    }

    private static void Connect(LayoutRoom from, LayoutRoom to, RoomDirection direction)
    {
        from.Connections |= direction.ToConnectionMask();
        to.Connections |= direction.Opposite().ToConnectionMask();
        to.Depth = from.Depth + 1;
    }

    private static void Shuffle<T>(List<T> values)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int other = Random.Range(0, i + 1);
            (values[i], values[other]) = (values[other], values[i]);
        }
    }

    private sealed class LayoutRoom
    {
        public readonly Vector2Int Position;
        public readonly RoomType Type;
        public RoomConnectionMask Connections;
        public RoomDirection ExitDirection;
        public int Depth;

        public LayoutRoom(Vector2Int position, RoomType type)
        {
            Position = position;
            Type = type;
        }
    }
}
