using System;
using System.Collections.Generic;
using System.Linq;
using Features.Enemies.Scripts;
using UnityEngine;
using Zenject;

public sealed class MinimapController : IDisposable, ITickable
{
    private readonly IRogueLikeRuntimeDataService _runtimeDataService;
    private readonly MinimapElementFactory _elementFactory;
    private readonly ICharacterProvider _characterProvider;
    private readonly IEnemiesProvider _enemiesProvider;
    private readonly MinimapChestMarkerController _chestMarkerController;
    private readonly Dictionary<RoomData, MinimapRoomIcon> _icons = new();
    private readonly Dictionary<Room, MinimapRoomIcon> _iconsByRoom = new();
    private readonly Dictionary<RoomData, Room> _roomViews = new();
    private readonly Dictionary<Vector2Int, RoomData> _roomsByPosition = new();
    private readonly Dictionary<RoomData, Vector2Int> _positionsByRoom = new();
    private readonly Dictionary<RoomData, MinimapRoomBounds> _boundsByRoom = new();
    private readonly Dictionary<Room, MinimapRoomBounds> _boundsByRoomView = new();
    private readonly HashSet<RoomData> _visitedRooms = new();
    private readonly HashSet<RoomData> _discoveredIconRooms = new();
    private readonly HashSet<RoomData> _visibleIconRooms = new();
    private readonly List<ConnectionEntry> _connections = new();

    private MinimapView _view;
    private LevelView _level;
    private RoomData _currentRoom;
    private readonly List<Vector2> _enemyPositions = new();

    public MinimapController(IRogueLikeRuntimeDataService runtimeDataService,
        MinimapElementFactory elementFactory, ICharacterProvider characterProvider,
        IEnemiesProvider enemiesProvider, MinimapChestMarkerController chestMarkerController)
    {
        _runtimeDataService = runtimeDataService;
        _elementFactory = elementFactory;
        _characterProvider = characterProvider;
        _enemiesProvider = enemiesProvider;
        _chestMarkerController = chestMarkerController;
        _runtimeDataService.RoomChanged += HandleRoomChanged;
    }

    public void Attach(MinimapView view)
    {
        _view = view != null ? view : throw new ArgumentNullException(nameof(view));

        if (_level != null)
            Build();
    }

    public void Detach(MinimapView view)
    {
        if (_view != view)
            return;

        ClearRuntimeView();
        _view = null;
    }

    public void SetLevel(LevelView level)
    {
        _level = level != null ? level : throw new ArgumentNullException(nameof(level));
        _currentRoom = null;
        _visitedRooms.Clear();
        _discoveredIconRooms.Clear();

        if (_view != null)
            Build();
    }

    public void Dispose()
    {
        _runtimeDataService.RoomChanged -= HandleRoomChanged;
    }

    public void Tick()
    {
        if (_view == null || _currentRoom == null ||
            !_icons.TryGetValue(_currentRoom, out MinimapRoomIcon currentIcon) ||
            !_roomViews.TryGetValue(_currentRoom, out Room currentRoomView))
            return;

        CharacterFacade character = _characterProvider.CharacterFacade;
        if (character == null)
            return;

        Vector3 localPosition =
            currentRoomView.transform.InverseTransformPoint(character.transform.position);
        Vector2 normalizedPosition = _boundsByRoom.TryGetValue(_currentRoom, out MinimapRoomBounds bounds)
            ? bounds.Normalize(localPosition)
            : NormalizeByDefaultRoomSize(localPosition);
        currentIcon.SetPlayerPosition(normalizedPosition);

        float mapRotation = character.CameraPivot.eulerAngles.y;
        currentIcon.SetPlayerRotation(-mapRotation);
        UpdateEnemyMarkers(currentIcon, currentRoomView);
        _view.Content.localRotation = Quaternion.Euler(0f, 0f, mapRotation);

        Vector2 roomPosition =
            currentIcon.GetComponent<RectTransform>().anchoredPosition;
        _view.Content.anchoredPosition = -Rotate(roomPosition, mapRotation);
    }

    private void Build()
    {
        ClearRuntimeView();

        var rooms = new List<RoomEntry>
        {
            new(_level.StartRoom.RoomData, _level.StartRoom,
                _level.StartRoomGridPosition,
                MinimapRoomKind.Start, null)
        };
        rooms.AddRange(_level.Rooms.Select(node =>
            new RoomEntry(node.Room.RoomData, node.Room, node.GridPosition,
                node.IsLevelExit ? MinimapRoomKind.Exit : MinimapRoomKind.Normal,
                node.IsLevelExit ? node.LevelExitDirection : null)));

        Vector2 center = CalculateCenter(rooms);
        foreach (RoomEntry room in rooms)
        {
            _roomsByPosition.Add(room.Position, room.Data);
            _positionsByRoom.Add(room.Data, room.Position);
            _roomViews.Add(room.Data, room.View);
            MinimapRoomBounds bounds = CalculateRoomBounds(room.View);
            _boundsByRoom.Add(room.Data, bounds);
            _boundsByRoomView.Add(room.View, bounds);
        }

        BuildConnections(rooms, center);

        foreach (RoomEntry room in rooms)
        {
            Vector2 position = ToUiPosition(room.Position, center);
            MinimapRoomIcon icon = _elementFactory.CreateRoom(_view, position);
            icon.SetKind(room.Kind, room.ExitDirection);
            _icons.Add(room.Data, icon);
            _iconsByRoom.Add(room.View, icon);
        }

        RoomData currentRoom = _runtimeDataService.CurrentRoomData;
        if (currentRoom != null && _positionsByRoom.ContainsKey(currentRoom))
            _currentRoom = currentRoom;

        _chestMarkerController.SetRooms(_iconsByRoom, _boundsByRoomView);
        SyncCurrentRoomMarkers();
        UpdateStates();
    }

    private void BuildConnections(IEnumerable<RoomEntry> rooms, Vector2 center)
    {
        foreach (RoomEntry room in rooms)
        {
            TryCreateRoomConnection(room, RoomDirection.Right, center);
            TryCreateRoomConnection(room, RoomDirection.Up, center);

            if (room.ExitDirection.HasValue)
                CreateLevelExitConnection(room, room.ExitDirection.Value, center);
        }
    }

    private void TryCreateRoomConnection(RoomEntry from, RoomDirection direction,
        Vector2 center)
    {
        bool hasPassage = from.View.RoomData.RoomDoors.Any(door =>
            door.Direction == direction && door.HasRoomDestination);
        if (!hasPassage)
            return;

        Vector2Int targetPosition = from.Position + direction.ToGridOffset();
        if (!_roomsByPosition.TryGetValue(targetPosition, out RoomData target))
            return;

        MinimapConnection connection = CreateConnection(
            from.Position, targetPosition, direction, center);
        _connections.Add(new ConnectionEntry(from.Data, target, connection));
    }

    private void CreateLevelExitConnection(RoomEntry room, RoomDirection direction,
        Vector2 center)
    {
        Vector2Int targetPosition = room.Position + direction.ToGridOffset();
        MinimapConnection connection = CreateConnection(
            room.Position, targetPosition, direction, center);
        _connections.Add(new ConnectionEntry(room.Data, null, connection));
    }

    private MinimapConnection CreateConnection(Vector2Int fromPosition,
        Vector2Int toPosition, RoomDirection direction, Vector2 center)
    {
        Vector2 fromUiPosition = ToUiPosition(fromPosition, center);
        Vector2 toUiPosition = ToUiPosition(toPosition, center);
        bool isHorizontal = direction is RoomDirection.Left or RoomDirection.Right;
        return _elementFactory.CreateConnection(
            _view, (fromUiPosition + toUiPosition) * 0.5f, isHorizontal);
    }

    private void HandleRoomChanged(RoomData previousRoom, RoomData currentRoom)
    {
        if (_level == null || !_positionsByRoom.ContainsKey(currentRoom))
            return;

        if (previousRoom != null && _positionsByRoom.ContainsKey(previousRoom))
            _visitedRooms.Add(previousRoom);

        _currentRoom = currentRoom;
        foreach (MinimapRoomIcon icon in _icons.Values)
            icon.SetEnemyPositions(null);

        SyncCurrentRoomMarkers();
        UpdateStates();
    }

    private void UpdateEnemyMarkers(MinimapRoomIcon currentIcon, Room currentRoomView)
    {
        _enemyPositions.Clear();
        IReadOnlyList<EnemyFacade> enemies = _enemiesProvider.ActiveEnemies;

        for (int index = 0; index < enemies.Count; index++)
        {
            EnemyFacade enemy = enemies[index];
            if (enemy == null || enemy.gameObject.activeInHierarchy == false)
                continue;

            Vector3 localPosition =
                currentRoomView.transform.InverseTransformPoint(enemy.transform.position);
            Vector2 normalizedPosition = _boundsByRoom.TryGetValue(_currentRoom, out MinimapRoomBounds bounds)
                ? bounds.Normalize(localPosition)
                : NormalizeByDefaultRoomSize(localPosition);

            _enemyPositions.Add(normalizedPosition);
        }

        currentIcon.SetEnemyPositions(_enemyPositions);
    }

    private void SyncCurrentRoomMarkers()
    {
        Room currentRoomView = _currentRoom != null &&
                               _roomViews.TryGetValue(_currentRoom, out Room roomView)
            ? roomView
            : null;
        _chestMarkerController.SetCurrentRoom(currentRoomView);
    }

    private void UpdateStates()
    {
        if (_view == null)
            return;

        var states = new Dictionary<RoomData, MinimapRoomState>();
        _visibleIconRooms.Clear();

        foreach (KeyValuePair<RoomData, MinimapRoomIcon> entry in _icons)
        {
            MinimapRoomState state = GetState(entry.Key);
            if (CanDiscoverRoomIcons(entry.Key))
                _discoveredIconRooms.Add(entry.Key);

            bool canShowRoomIcons = _discoveredIconRooms.Contains(entry.Key);

            if (canShowRoomIcons)
                _visibleIconRooms.Add(entry.Key);

            states.Add(entry.Key, state);
            entry.Value.SetState(state);
            entry.Value.SetRoomKindMarkerVisible(canShowRoomIcons);
            entry.Value.SetCombatRoomMarkerVisible(
                canShowRoomIcons &&
                state == MinimapRoomState.Available &&
                entry.Key is DefaultEnemiesRoomData { IsCompleted: false });
        }

        _chestMarkerController.SetVisibleRooms(_visibleIconRooms);

        foreach (ConnectionEntry connection in _connections)
        {
            MinimapRoomState fromState = states[connection.From];
            MinimapRoomState toState = connection.To != null
                ? states[connection.To]
                : MinimapRoomState.Hidden;
            bool isVisible = true;
            bool isHighlighted = fromState == MinimapRoomState.Current ||
                                 toState == MinimapRoomState.Current;
            connection.View.SetState(isVisible, isHighlighted);
        }
    }

    private MinimapRoomState GetState(RoomData room)
    {
        if (ReferenceEquals(room, _currentRoom))
            return MinimapRoomState.Current;

        if (_visitedRooms.Contains(room))
            return MinimapRoomState.Visited;

        return MinimapRoomState.Available;
    }

    private bool CanDiscoverRoomIcons(RoomData room)
    {
        if (_currentRoom == null || room == null)
            return false;

        if (ReferenceEquals(room, _currentRoom))
            return true;

        return IsConnectedToCurrentRoom(room);
    }

    private bool IsConnectedToCurrentRoom(RoomData room)
    {
        foreach (ConnectionEntry connection in _connections)
        {
            if (connection.To == null)
                continue;

            bool fromCurrentToRoom =
                ReferenceEquals(connection.From, _currentRoom) &&
                ReferenceEquals(connection.To, room);
            bool fromRoomToCurrent =
                ReferenceEquals(connection.From, room) &&
                ReferenceEquals(connection.To, _currentRoom);

            if (fromCurrentToRoom || fromRoomToCurrent)
                return true;
        }

        return false;
    }

    private Vector2 ToUiPosition(Vector2Int gridPosition, Vector2 center) =>
        ((Vector2)gridPosition - center) * _view.CellSize;

    private static Vector2 Rotate(Vector2 point, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(
            point.x * cos - point.y * sin,
            point.x * sin + point.y * cos);
    }

    private static Vector2 CalculateCenter(IReadOnlyCollection<RoomEntry> rooms)
    {
        float minX = rooms.Min(room => room.Position.x);
        float maxX = rooms.Max(room => room.Position.x);
        float minY = rooms.Min(room => room.Position.y);
        float maxY = rooms.Max(room => room.Position.y);
        return new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
    }

    private static Vector2 NormalizeByDefaultRoomSize(Vector3 localPosition)
    {
        float roomHalfSize = LevelView.RoomWorldSize * 0.5f;
        return new Vector2(localPosition.x / roomHalfSize, localPosition.z / roomHalfSize);
    }

    private static MinimapRoomBounds CalculateRoomBounds(Room room)
    {
        Renderer[] renderers = room.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return MinimapRoomBounds.Default;

        bool hasBounds = false;
        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minZ = float.PositiveInfinity;
        float maxZ = float.NegativeInfinity;

        foreach (Renderer renderer in renderers)
        {
            Bounds worldBounds = renderer.bounds;
            if (worldBounds.size.sqrMagnitude <= Mathf.Epsilon)
                continue;

            Include(renderer.transform, room.transform,
                worldBounds.min.x, worldBounds.min.z,
                ref minX, ref maxX, ref minZ, ref maxZ, ref hasBounds);
            Include(renderer.transform, room.transform,
                worldBounds.min.x, worldBounds.max.z,
                ref minX, ref maxX, ref minZ, ref maxZ, ref hasBounds);
            Include(renderer.transform, room.transform,
                worldBounds.max.x, worldBounds.min.z,
                ref minX, ref maxX, ref minZ, ref maxZ, ref hasBounds);
            Include(renderer.transform, room.transform,
                worldBounds.max.x, worldBounds.max.z,
                ref minX, ref maxX, ref minZ, ref maxZ, ref hasBounds);
        }

        if (!hasBounds || maxX - minX <= Mathf.Epsilon || maxZ - minZ <= Mathf.Epsilon)
            return MinimapRoomBounds.Default;

        return new MinimapRoomBounds(minX, maxX, minZ, maxZ);
    }

    private static void Include(Transform rendererTransform, Transform roomTransform,
        float worldX, float worldZ, ref float minX, ref float maxX,
        ref float minZ, ref float maxZ, ref bool hasBounds)
    {
        Vector3 localPoint = roomTransform.InverseTransformPoint(
            new Vector3(worldX, rendererTransform.position.y, worldZ));
        minX = Mathf.Min(minX, localPoint.x);
        maxX = Mathf.Max(maxX, localPoint.x);
        minZ = Mathf.Min(minZ, localPoint.z);
        maxZ = Mathf.Max(maxZ, localPoint.z);
        hasBounds = true;
    }

    private void ClearRuntimeView()
    {
        _chestMarkerController.Clear();

        if (_view != null)
            _view.Clear();

        _icons.Clear();
        _iconsByRoom.Clear();
        _roomViews.Clear();
        _roomsByPosition.Clear();
        _positionsByRoom.Clear();
        _boundsByRoom.Clear();
        _boundsByRoomView.Clear();
        _visibleIconRooms.Clear();
        _connections.Clear();
    }

    private readonly struct RoomEntry
    {
        public RoomData Data { get; }
        public Room View { get; }
        public Vector2Int Position { get; }
        public MinimapRoomKind Kind { get; }
        public RoomDirection? ExitDirection { get; }

        public RoomEntry(RoomData data, Room view, Vector2Int position,
            MinimapRoomKind kind, RoomDirection? exitDirection)
        {
            Data = data;
            View = view;
            Position = position;
            Kind = kind;
            ExitDirection = exitDirection;
        }
    }

    private readonly struct ConnectionEntry
    {
        public RoomData From { get; }
        public RoomData To { get; }
        public MinimapConnection View { get; }

        public ConnectionEntry(RoomData from, RoomData to, MinimapConnection view)
        {
            From = from;
            To = to;
            View = view;
        }
    }
}
