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

        var nodes = new LevelRoomNode[layout.Count];
        for (int i = 0; i < layout.Count; i++)
        {
            LayoutRoom room = layout[i];
            List<LevelRoomNode> compatible = GetCompatibleTemplates(templates, room.Type, room.Connections);
            if (compatible.Count == 0)
                throw new InvalidOperationException(
                    $"No {room.Type} prefab fits generated room {room.Position} ({room.Connections}).");

            LevelRoomNode source = compatible[Random.Range(0, compatible.Count)];
            nodes[i] = new LevelRoomNode(source.RoomPrefab, room.Position, room.Type,
                room.ExitDirection, source.EnemySettings, RoomConnectionMask.All & ~room.Connections);
        }

        return nodes;
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
        var candidates = new List<(LayoutRoom Parent, RoomDirection Direction)>();
        foreach (LayoutRoom parent in layout)
        {
            if (parent.Type != RoomType.Enemy)
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
            throw new InvalidOperationException(
                $"Cannot attach a generated {type} room. Reduce branch counts or add compatible room prefabs.");

        var selected = candidates[Random.Range(0, candidates.Count)];
        var branch = new LayoutRoom(selected.Parent.Position + selected.Direction.ToGridOffset(), type);
        Connect(selected.Parent, branch, selected.Direction);
        layout.Add(branch);
        occupied.Add(branch.Position);
    }

    private static List<LevelRoomNode> GetCompatibleTemplates(
        Dictionary<RoomType, List<LevelRoomNode>> templates, RoomType type, RoomConnectionMask connections)
    {
        var result = new List<LevelRoomNode>();
        if (!templates.TryGetValue(type, out List<LevelRoomNode> pool))
            return result;

        var required = new List<RoomDirection>();
        foreach (RoomDirection direction in Directions)
            if ((connections & direction.ToConnectionMask()) != RoomConnectionMask.None)
                required.Add(direction);
        foreach (LevelRoomNode node in pool)
            if (LevelView.CanFitRoom(node.RoomPrefab, required))
                result.Add(node);
        return result;
    }

    private static void Connect(LayoutRoom from, LayoutRoom to, RoomDirection direction)
    {
        from.Connections |= direction.ToConnectionMask();
        to.Connections |= direction.Opposite().ToConnectionMask();
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

        public LayoutRoom(Vector2Int position, RoomType type)
        {
            Position = position;
            Type = type;
        }
    }
}
