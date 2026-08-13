#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelView))]
[CanEditMultipleObjects]
public sealed class LevelViewEditor : Editor
{
    private const float CanvasPadding = 8f;
    private const float CellHeight = 58f;
    private const float CellGap = 12f;
    private const float MinCellWidth = 46f;
    private const float MaxCellWidth = 112f;
    private const float HorizontalScrollbarHeight = 18f;

    private static readonly RoomDirection[] ConnectionDirections =
    {
        RoomDirection.Right,
        RoomDirection.Up
    };

    private bool _showLevelScheme = true;
    private Vector2 _schemeScrollPosition;

    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "Assign the start-room prefab and room prefabs with their Grid Position. " +
            "The runtime creates room instances, disables their authored doors, and enables only " +
            "the doors assigned in RoomData and required by the grid topology. Each room prefab must already contain " +
            "its directional RoomDoor objects; no doors are instantiated or replaced. " +
            "Each RoomDoor selects EnemyDoor or RewardDoor from the destination room type. " +
            "Enemy and Exit rooms keep their enemy types and spawn settings directly on their room node.",
            MessageType.Info);

        DrawDefaultInspector();

        EditorGUILayout.Space();
        if (GUILayout.Button("Validate Level Setup"))
        {
            foreach (UnityEngine.Object selectedTarget in targets)
            {
                var level = (LevelView)selectedTarget;
                try
                {
                    level.ValidateAuthoring();
                    Debug.Log($"{level.name}: level setup is valid.", level);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, level);
                }
            }
        }

        DrawLevelScheme();
    }

    private void DrawLevelScheme()
    {
        EditorGUILayout.Space();
        _showLevelScheme = EditorGUILayout.Foldout(
            _showLevelScheme, "Level Scheme", true, EditorStyles.foldoutHeader);
        if (!_showLevelScheme)
            return;

        if (targets.Length != 1)
        {
            EditorGUILayout.HelpBox(
                "Select one LevelView to display its room scheme.",
                MessageType.Info);
            return;
        }

        var level = (LevelView)target;
        IReadOnlyList<LevelRoomNode> levelRooms = level.Rooms;
        if (levelRooms == null || levelRooms.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Add rooms to display the level scheme.",
                MessageType.Info);
            return;
        }

        var issues = new List<string>();
        List<SchemeRoom> rooms = BuildSchemeRooms(levelRooms, issues);
        if (rooms.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "The room array does not contain drawable room nodes.",
                MessageType.Warning);
            DrawIssues(issues);
            return;
        }

        Dictionary<Vector2Int, SchemeRoom> roomsByPosition =
            BuildRoomPositionMap(rooms, issues);
        List<SchemeConnection> connections =
            BuildConnections(roomsByPosition, issues);

        SchemeRoom startRoom = GetSingleRoomOfType(
            rooms, RoomType.Start, "Start", issues);
        SchemeRoom exitRoom = GetSingleRoomOfType(
            rooms, RoomType.Exit, "Exit", issues);
        bool hasValidLevelExit = ValidateLevelExit(
            exitRoom, roomsByPosition, issues);

        HashSet<long> shortestPathEdges = new();
        List<SchemeRoom> shortestPath = CalculateShortestPath(
            rooms, connections, startRoom, exitRoom, shortestPathEdges);

        DrawSchemeCanvas(
            rooms, roomsByPosition, connections, exitRoom, shortestPathEdges,
            shortestPath != null && hasValidLevelExit,
            hasValidLevelExit);

        EditorGUILayout.LabelField(
            "#N is the room index in the Rooms array. Step is the minimum number of transitions from Start.",
            EditorStyles.wordWrappedMiniLabel);
        DrawConnectionLegend();

        if (shortestPath != null && hasValidLevelExit)
        {
            string route = string.Join("  →  ", shortestPath.Select(room =>
                $"#{room.Index} {GetRoomTypeLabel(room.Node.Type)}"));
            EditorGUILayout.HelpBox(
                $"Shortest route to the level exit:\n{route}  →  Level Exit",
                MessageType.Info);
        }
        else if (shortestPath != null)
        {
            string route = string.Join("  →  ", shortestPath.Select(room =>
                $"#{room.Index} {GetRoomTypeLabel(room.Node.Type)}"));
            EditorGUILayout.HelpBox(
                $"The route reaches the Exit room, but the level-exit connection is invalid:\n{route}",
                MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "There is no valid connected route from Start to Exit.",
                MessageType.Warning);
        }

        DrawIssues(issues);
    }

    private static List<SchemeRoom> BuildSchemeRooms(
        IReadOnlyList<LevelRoomNode> source, ICollection<string> issues)
    {
        var result = new List<SchemeRoom>(source.Count);
        for (int index = 0; index < source.Count; index++)
        {
            LevelRoomNode node = source[index];
            if (node == null)
            {
                issues.Add($"Rooms[{index}] is missing.");
                continue;
            }

            var room = new SchemeRoom(index, node);
            result.Add(room);

            Room roomPrefab = node.RoomPrefab;
            if (roomPrefab == null)
            {
                issues.Add($"Rooms[{index}] does not have a room prefab.");
                continue;
            }

            RoomData roomData = roomPrefab.RoomData;
            if (roomData == null)
            {
                issues.Add($"Rooms[{index}] ({roomPrefab.name}) does not have RoomData.");
                continue;
            }

            RoomDoor[] doors = roomData.RoomDoors;
            if (doors == null || doors.Length == 0)
            {
                issues.Add($"Rooms[{index}] ({roomPrefab.name}) does not have active doors.");
                continue;
            }

            foreach (RoomDoor door in doors)
            {
                if (door == null)
                {
                    issues.Add($"Rooms[{index}] ({roomPrefab.name}) contains a missing door.");
                    continue;
                }

                if (!room.Doors.Add(door.Direction))
                {
                    issues.Add(
                        $"Rooms[{index}] ({roomPrefab.name}) contains duplicate {door.Direction} doors.");
                }
            }
        }

        return result;
    }

    private static Dictionary<Vector2Int, SchemeRoom> BuildRoomPositionMap(
        IEnumerable<SchemeRoom> rooms, ICollection<string> issues)
    {
        var result = new Dictionary<Vector2Int, SchemeRoom>();
        foreach (SchemeRoom room in rooms)
        {
            if (result.TryAdd(room.Node.GridPosition, room))
                continue;

            SchemeRoom existing = result[room.Node.GridPosition];
            issues.Add(
                $"Rooms[{existing.Index}] and Rooms[{room.Index}] use the same grid position {room.Node.GridPosition}.");
        }

        return result;
    }

    private static List<SchemeConnection> BuildConnections(
        IReadOnlyDictionary<Vector2Int, SchemeRoom> roomsByPosition,
        ICollection<string> issues)
    {
        var result = new List<SchemeConnection>();
        foreach (SchemeRoom room in roomsByPosition.Values)
        {
            foreach (RoomDirection direction in ConnectionDirections)
            {
                Vector2Int neighbourPosition =
                    room.Node.GridPosition + direction.ToGridOffset();
                if (!roomsByPosition.TryGetValue(neighbourPosition,
                        out SchemeRoom neighbour))
                    continue;

                bool hasOutgoingDoor = room.Doors.Contains(direction);
                bool hasIncomingDoor =
                    neighbour.Doors.Contains(direction.Opposite());
                if (!hasOutgoingDoor && !hasIncomingDoor)
                    continue;

                bool isValid = hasOutgoingDoor && hasIncomingDoor;
                result.Add(new SchemeConnection(room, neighbour, isValid));
                if (!isValid)
                {
                    issues.Add(
                        $"Rooms[{room.Index}] and Rooms[{neighbour.Index}] are adjacent but do not have matching doors.");
                }
            }
        }

        return result;
    }

    private static SchemeRoom GetSingleRoomOfType(
        IReadOnlyCollection<SchemeRoom> rooms, RoomType type,
        string displayName, ICollection<string> issues)
    {
        SchemeRoom[] matches = rooms
            .Where(room => room.Node.Type == type)
            .ToArray();
        if (matches.Length == 1)
            return matches[0];

        issues.Add(
            $"The level must contain exactly one {displayName} room, but contains {matches.Length}.");
        return null;
    }

    private static bool ValidateLevelExit(
        SchemeRoom exitRoom,
        IReadOnlyDictionary<Vector2Int, SchemeRoom> roomsByPosition,
        ICollection<string> issues)
    {
        if (exitRoom == null)
            return false;

        RoomDirection direction = exitRoom.Node.LevelExitDirection;
        bool hasExitDoor = exitRoom.Doors.Contains(direction);
        if (!hasExitDoor)
        {
            issues.Add(
                $"Rooms[{exitRoom.Index}] does not have its configured {direction} level-exit door.");
        }

        Vector2Int destination =
            exitRoom.Node.GridPosition + direction.ToGridOffset();
        bool exitPositionIsFree = !roomsByPosition.TryGetValue(
            destination, out SchemeRoom blockingRoom);
        if (!exitPositionIsFree)
        {
            issues.Add(
                $"The level exit from Rooms[{exitRoom.Index}] points into Rooms[{blockingRoom.Index}].");
        }

        return hasExitDoor && exitPositionIsFree;
    }

    private static List<SchemeRoom> CalculateShortestPath(
        IEnumerable<SchemeRoom> rooms,
        IEnumerable<SchemeConnection> connections,
        SchemeRoom startRoom,
        SchemeRoom exitRoom,
        ISet<long> shortestPathEdges)
    {
        var adjacency = new Dictionary<SchemeRoom, List<SchemeRoom>>();
        foreach (SchemeRoom room in rooms)
        {
            room.DistanceFromStart = -1;
            room.Previous = null;
            adjacency[room] = new List<SchemeRoom>();
        }

        foreach (SchemeConnection connection in connections)
        {
            if (!connection.IsValid)
                continue;

            adjacency[connection.From].Add(connection.To);
            adjacency[connection.To].Add(connection.From);
        }

        if (startRoom == null || exitRoom == null)
            return null;

        var pending = new Queue<SchemeRoom>();
        startRoom.DistanceFromStart = 0;
        pending.Enqueue(startRoom);

        while (pending.Count > 0)
        {
            SchemeRoom current = pending.Dequeue();
            foreach (SchemeRoom neighbour in adjacency[current]
                         .OrderBy(room => room.Index))
            {
                if (neighbour.DistanceFromStart >= 0)
                    continue;

                neighbour.DistanceFromStart = current.DistanceFromStart + 1;
                neighbour.Previous = current;
                pending.Enqueue(neighbour);
            }
        }

        if (exitRoom.DistanceFromStart < 0)
            return null;

        var path = new List<SchemeRoom>();
        for (SchemeRoom current = exitRoom; current != null; current = current.Previous)
        {
            path.Add(current);
            if (current.Previous != null)
            {
                shortestPathEdges.Add(
                    GetConnectionKey(current.Index, current.Previous.Index));
            }
        }

        path.Reverse();
        return path;
    }

    private void DrawSchemeCanvas(
        IReadOnlyCollection<SchemeRoom> rooms,
        IReadOnlyDictionary<Vector2Int, SchemeRoom> roomsByPosition,
        IReadOnlyCollection<SchemeConnection> connections,
        SchemeRoom exitRoom,
        ISet<long> shortestPathEdges,
        bool hasCompleteRoute,
        bool hasValidLevelExit)
    {
        var positions = rooms.Select(room => room.Node.GridPosition).ToList();
        Vector2Int? levelExitPosition = null;
        if (exitRoom != null)
        {
            levelExitPosition = exitRoom.Node.GridPosition +
                                exitRoom.Node.LevelExitDirection.ToGridOffset();
            positions.Add(levelExitPosition.Value);
        }

        int minX = positions.Min(position => position.x);
        int maxX = positions.Max(position => position.x);
        int minY = positions.Min(position => position.y);
        int maxY = positions.Max(position => position.y);
        int columnCount = maxX - minX + 1;
        int rowCount = maxY - minY + 1;

        float estimatedViewportWidth = Mathf.Max(
            120f, EditorGUIUtility.currentViewWidth - 40f);
        float fittedCellWidth =
            (estimatedViewportWidth - CanvasPadding * 2f -
             Mathf.Max(0, columnCount - 1) * CellGap) / columnCount;
        float cellWidth = Mathf.Clamp(
            fittedCellWidth, MinCellWidth, MaxCellWidth);
        float contentWidth = CanvasPadding * 2f + columnCount * cellWidth +
                             Mathf.Max(0, columnCount - 1) * CellGap;
        float contentHeight = CanvasPadding * 2f + rowCount * CellHeight +
                              Mathf.Max(0, rowCount - 1) * CellGap;

        Rect viewport = GUILayoutUtility.GetRect(
            GUIContent.none, GUIStyle.none,
            GUILayout.Height(contentHeight + HorizontalScrollbarHeight),
            GUILayout.ExpandWidth(true));
        var contentRect = new Rect(
            0f, 0f, Mathf.Max(viewport.width, contentWidth), contentHeight);
        _schemeScrollPosition = GUI.BeginScrollView(
            viewport, _schemeScrollPosition, contentRect,
            false, false);
        try
        {
            float originX = CanvasPadding;
            float originY = CanvasPadding;

            var roomRects = new Dictionary<SchemeRoom, Rect>();
            foreach (SchemeRoom room in rooms)
            {
                roomRects[room] = GetGridRect(
                    room.Node.GridPosition, minX, maxY,
                    originX, originY, cellWidth);
            }

            Rect levelExitRect = default;
            if (levelExitPosition.HasValue)
            {
                Rect exitCell = GetGridRect(
                    levelExitPosition.Value, minX, maxY,
                    originX, originY, cellWidth);
                float markerWidth = Mathf.Min(cellWidth, 72f);
                levelExitRect = new Rect(
                    exitCell.center.x - markerWidth * 0.5f,
                    exitCell.center.y - 17f,
                    markerWidth,
                    34f);
            }

            DrawConnections(
                connections, roomRects, shortestPathEdges,
                exitRoom, levelExitPosition, levelExitRect,
                roomsByPosition, hasCompleteRoute);

            foreach (SchemeRoom room in rooms.OrderBy(room => room.Index))
            {
                bool isOnShortestPath = room.DistanceFromStart >= 0 &&
                                        (room == exitRoom ||
                                         shortestPathEdges.Any(key =>
                                             ConnectionContainsRoom(key, room.Index)));
                DrawRoomNode(roomRects[room], room, isOnShortestPath);
            }

            bool levelExitPositionIsFree = levelExitPosition.HasValue &&
                                           !roomsByPosition.ContainsKey(
                                               levelExitPosition.Value);
            if (levelExitPositionIsFree)
            {
                DrawLevelExitNode(
                    levelExitRect, exitRoom, hasValidLevelExit);
            }
        }
        finally
        {
            GUI.EndScrollView();
        }
    }

    private static Rect GetGridRect(
        Vector2Int position, int minX, int maxY,
        float originX, float originY, float cellWidth)
    {
        int column = position.x - minX;
        int row = maxY - position.y;
        return new Rect(
            originX + column * (cellWidth + CellGap),
            originY + row * (CellHeight + CellGap),
            cellWidth,
            CellHeight);
    }

    private static void DrawConnections(
        IEnumerable<SchemeConnection> connections,
        IReadOnlyDictionary<SchemeRoom, Rect> roomRects,
        ISet<long> shortestPathEdges,
        SchemeRoom exitRoom,
        Vector2Int? levelExitPosition,
        Rect levelExitRect,
        IReadOnlyDictionary<Vector2Int, SchemeRoom> roomsByPosition,
        bool hasCompleteRoute)
    {
        foreach (SchemeConnection connection in connections)
        {
            Vector2 from = roomRects[connection.From].center;
            Vector2 to = roomRects[connection.To].center;
            if (!connection.IsValid)
            {
                DrawConnectionLine(
                    from, to, GetInvalidConnectionColor(), 2f, true);
                continue;
            }

            bool isShortestPath = shortestPathEdges.Contains(
                GetConnectionKey(connection.From.Index, connection.To.Index));
            DrawConnectionLine(
                from, to,
                isShortestPath
                    ? GetShortestPathColor()
                    : GetConnectionColor(),
                isShortestPath ? 5f : 2f,
                false);
        }

        if (exitRoom == null || !levelExitPosition.HasValue)
            return;

        Vector2 exitFrom = roomRects[exitRoom].center;
        Vector2 exitTo = levelExitRect.center;
        bool hasExitDoor = exitRoom.Doors.Contains(
            exitRoom.Node.LevelExitDirection);
        bool exitPositionIsFree =
            !roomsByPosition.ContainsKey(levelExitPosition.Value);
        bool isValidExit = hasExitDoor && exitPositionIsFree;
        Color exitColor = !isValidExit
            ? GetInvalidConnectionColor()
            : hasCompleteRoute
                ? GetShortestPathColor()
                : GetConnectionColor();

        DrawConnectionLine(
            exitFrom, exitTo, exitColor,
            isValidExit && hasCompleteRoute ? 5f : 2f,
            !isValidExit);
        DrawDirectionArrow(exitFrom, exitTo, exitColor);
    }

    private static void DrawConnectionLine(
        Vector2 from, Vector2 to, Color color,
        float thickness, bool dotted)
    {
        bool isHorizontal = Mathf.Abs(to.x - from.x) >=
                            Mathf.Abs(to.y - from.y);
        float start = isHorizontal
            ? Mathf.Min(from.x, to.x)
            : Mathf.Min(from.y, to.y);
        float end = isHorizontal
            ? Mathf.Max(from.x, to.x)
            : Mathf.Max(from.y, to.y);
        float length = end - start;
        if (length <= Mathf.Epsilon)
            return;

        if (!dotted)
        {
            Rect line = isHorizontal
                ? new Rect(start, from.y - thickness * 0.5f, length, thickness)
                : new Rect(from.x - thickness * 0.5f, start, thickness, length);
            EditorGUI.DrawRect(line, color);
            return;
        }

        const float dashLength = 4f;
        const float dashSpacing = 3f;
        for (float position = start; position < end;
             position += dashLength + dashSpacing)
        {
            float currentLength = Mathf.Min(dashLength, end - position);
            Rect dash = isHorizontal
                ? new Rect(
                    position, from.y - thickness * 0.5f,
                    currentLength, thickness)
                : new Rect(
                    from.x - thickness * 0.5f, position,
                    thickness, currentLength);
            EditorGUI.DrawRect(dash, color);
        }
    }

    private static void DrawDirectionArrow(
        Vector2 from, Vector2 to, Color color)
    {
        Vector2 delta = to - from;
        if (delta.sqrMagnitude <= Mathf.Epsilon)
            return;

        string arrow = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
            ? delta.x >= 0f ? "▶" : "◀"
            : delta.y >= 0f ? "▼" : "▲";
        Vector2 center = Vector2.Lerp(from, to, 0.58f);
        var rect = new Rect(center.x - 8f, center.y - 8f, 16f, 16f);
        var style = new GUIStyle(EditorStyles.miniBoldLabel)
        {
            alignment = TextAnchor.MiddleCenter
        };
        style.normal.textColor = color;
        GUI.Label(rect, arrow, style);
    }

    private static void DrawRoomNode(
        Rect rect, SchemeRoom room, bool isOnShortestPath)
    {
        Color fill = GetRoomColor(room.Node.Type);
        EditorGUI.DrawRect(rect, fill);
        DrawBorder(rect, isOnShortestPath
            ? GetShortestPathColor()
            : GetNodeBorderColor(), isOnShortestPath ? 3f : 1f);

        string type = GetRoomTypeLabel(room.Node.Type).ToUpperInvariant();
        string step = room.DistanceFromStart >= 0
            ? room.DistanceFromStart.ToString()
            : "—";
        string prefabName = room.Node.RoomPrefab != null
            ? room.Node.RoomPrefab.name
            : "<missing prefab>";

        string label = rect.width >= 78f
            ? $"#{room.Index}  {type}\n{Shorten(prefabName, 16)}\n" +
              $"({room.Node.GridPosition.x}, {room.Node.GridPosition.y})  ·  Step {step}"
            : $"#{room.Index}\n{type}\nStep {step}";
        string tooltip =
            $"Rooms[{room.Index}]\n" +
            $"Type: {GetRoomTypeLabel(room.Node.Type)}\n" +
            $"Prefab: {prefabName}\n" +
            $"Grid: ({room.Node.GridPosition.x}, {room.Node.GridPosition.y})\n" +
            $"Minimum step from Start: {step}";

        var style = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            clipping = TextClipping.Clip,
            wordWrap = false
        };
        style.normal.textColor = GetTextColor(fill);
        GUI.Label(rect, new GUIContent(label, tooltip), style);
    }

    private static void DrawLevelExitNode(
        Rect rect, SchemeRoom exitRoom, bool hasValidLevelExit)
    {
        Color fill = EditorGUIUtility.isProSkin
            ? new Color(0.18f, 0.32f, 0.2f, 1f)
            : new Color(0.72f, 0.9f, 0.74f, 1f);
        EditorGUI.DrawRect(rect, fill);
        DrawBorder(rect, hasValidLevelExit
            ? GetShortestPathColor()
            : GetInvalidConnectionColor(), 2f);

        var style = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            clipping = TextClipping.Clip
        };
        style.normal.textColor = GetTextColor(fill);

        string direction = exitRoom != null
            ? exitRoom.Node.LevelExitDirection.ToString()
            : "Unknown";
        GUI.Label(rect,
            new GUIContent("LEVEL\nEXIT", $"Exit direction: {direction}"),
            style);
    }

    private static void DrawBorder(Rect rect, Color color, float thickness)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(
            rect.x, rect.yMax - thickness, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        EditorGUI.DrawRect(new Rect(
            rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    private static void DrawConnectionLegend()
    {
        DrawLegendLine(GetShortestPathColor(), 5f,
            "Shortest connected route from Start to Exit", false);
        DrawLegendLine(GetConnectionColor(), 2f,
            "Available two-way room connection", false);
        DrawLegendLine(GetInvalidConnectionColor(), 2f,
            "Invalid or incomplete door connection", true);
    }

    private static void DrawLegendLine(
        Color color, float width, string label, bool dotted)
    {
        Rect row = EditorGUILayout.GetControlRect(
            false, EditorGUIUtility.singleLineHeight);
        Vector2 from = new(row.x + 4f, row.center.y);
        Vector2 to = new(row.x + 34f, row.center.y);
        DrawConnectionLine(from, to, color, width, dotted);

        GUI.Label(new Rect(
                row.x + 42f, row.y, Mathf.Max(0f, row.width - 42f), row.height),
            label, EditorStyles.miniLabel);
    }

    private static void DrawIssues(IReadOnlyCollection<string> issues)
    {
        if (issues.Count == 0)
            return;

        string message = string.Join("\n", issues.Select(issue => $"• {issue}"));
        EditorGUILayout.HelpBox(message, MessageType.Warning);
    }

    private static string Shorten(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value.Substring(0, Mathf.Max(1, maxLength - 1)) + "…";
    }

    private static string GetRoomTypeLabel(RoomType type) =>
        type switch
        {
            RoomType.Start => "Start",
            RoomType.Exit => "Exit",
            RoomType.Enemy => "Enemy",
            RoomType.Reward => "Reward",
            RoomType.Shop => "Shop",
            _ => "Unknown"
        };

    private static long GetConnectionKey(int firstIndex, int secondIndex)
    {
        int min = Mathf.Min(firstIndex, secondIndex);
        int max = Mathf.Max(firstIndex, secondIndex);
        return ((long)min << 32) | (uint)max;
    }

    private static bool ConnectionContainsRoom(long key, int roomIndex) =>
        (int)(key >> 32) == roomIndex || (int)(uint)key == roomIndex;

    private static Color GetRoomColor(RoomType type)
    {
        if (EditorGUIUtility.isProSkin)
        {
            return type switch
            {
                RoomType.Start => new Color(0.08f, 0.4f, 0.47f, 1f),
                RoomType.Exit => new Color(0.13f, 0.45f, 0.22f, 1f),
                RoomType.Reward => new Color(0.5f, 0.38f, 0.07f, 1f),
                RoomType.Shop => new Color(0.42f, 0.18f, 0.48f, 1f),
                _ => new Color(0.25f, 0.27f, 0.3f, 1f)
            };
        }

        return type switch
        {
            RoomType.Start => new Color(0.55f, 0.87f, 0.91f, 1f),
            RoomType.Exit => new Color(0.58f, 0.84f, 0.62f, 1f),
            RoomType.Reward => new Color(0.94f, 0.82f, 0.47f, 1f),
            RoomType.Shop => new Color(0.82f, 0.63f, 0.87f, 1f),
            _ => new Color(0.78f, 0.8f, 0.83f, 1f)
        };
    }

    private static Color GetTextColor(Color background)
    {
        float luminance = background.r * 0.299f +
                          background.g * 0.587f +
                          background.b * 0.114f;
        return luminance > 0.55f ? Color.black : Color.white;
    }

    private static Color GetConnectionColor() =>
        EditorGUIUtility.isProSkin
            ? new Color(0.62f, 0.65f, 0.7f, 1f)
            : new Color(0.32f, 0.35f, 0.4f, 1f);

    private static Color GetShortestPathColor() =>
        EditorGUIUtility.isProSkin
            ? new Color(0.32f, 0.82f, 0.4f, 1f)
            : new Color(0.08f, 0.55f, 0.16f, 1f);

    private static Color GetInvalidConnectionColor() =>
        EditorGUIUtility.isProSkin
            ? new Color(0.95f, 0.34f, 0.3f, 1f)
            : new Color(0.75f, 0.08f, 0.06f, 1f);

    private static Color GetNodeBorderColor() =>
        EditorGUIUtility.isProSkin
            ? new Color(0.08f, 0.09f, 0.1f, 1f)
            : new Color(0.28f, 0.3f, 0.34f, 1f);

    private sealed class SchemeRoom
    {
        public readonly int Index;
        public readonly LevelRoomNode Node;
        public readonly HashSet<RoomDirection> Doors = new();

        public int DistanceFromStart = -1;
        public SchemeRoom Previous;

        public SchemeRoom(int index, LevelRoomNode node)
        {
            Index = index;
            Node = node;
        }
    }

    private readonly struct SchemeConnection
    {
        public readonly SchemeRoom From;
        public readonly SchemeRoom To;
        public readonly bool IsValid;

        public SchemeConnection(
            SchemeRoom from, SchemeRoom to, bool isValid)
        {
            From = from;
            To = to;
            IsValid = isValid;
        }
    }
}
#endif
