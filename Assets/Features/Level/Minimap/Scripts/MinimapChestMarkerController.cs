using System;
using System.Collections.Generic;
using Features.Relics.Scripts;
using UnityEngine;

public sealed class MinimapChestMarkerController : IDisposable
{
    private readonly RelicChestSpawner _relicChestSpawner;
    private readonly RelicEventBus _relicEventBus;

    private IReadOnlyDictionary<Room, MinimapRoomIcon> _iconsByRoom =
        new Dictionary<Room, MinimapRoomIcon>();
    private IReadOnlyDictionary<Room, MinimapRoomBounds> _boundsByRoom =
        new Dictionary<Room, MinimapRoomBounds>();
    private Room _currentRoom;

    public MinimapChestMarkerController(RelicChestSpawner relicChestSpawner,
        RelicEventBus relicEventBus)
    {
        _relicChestSpawner = relicChestSpawner;
        _relicEventBus = relicEventBus;
        _relicEventBus.ChestsCleared += HandleChestsCleared;
        _relicEventBus.ChestSpawned += HandleChestChanged;
        _relicEventBus.ChestCollected += HandleChestCollected;
    }

    public void SetRooms(IReadOnlyDictionary<Room, MinimapRoomIcon> iconsByRoom,
        IReadOnlyDictionary<Room, MinimapRoomBounds> boundsByRoom)
    {
        _iconsByRoom = iconsByRoom ?? new Dictionary<Room, MinimapRoomIcon>();
        _boundsByRoom = boundsByRoom ?? new Dictionary<Room, MinimapRoomBounds>();
        Refresh();
    }

    public void SetCurrentRoom(Room currentRoom)
    {
        _currentRoom = currentRoom;
        Refresh();
    }

    public void Clear()
    {
        foreach (MinimapRoomIcon icon in _iconsByRoom.Values)
        {
            if (icon != null)
                icon.SetChestVisible(false);
        }
    }

    public void Refresh()
    {
        Clear();

        if (_currentRoom == null ||
            _iconsByRoom.TryGetValue(_currentRoom, out MinimapRoomIcon currentIcon) == false)
        {
            return;
        }

        IReadOnlyList<RelicChest> activeChests = _relicChestSpawner.ActiveChests;
        for (int index = 0; index < activeChests.Count; index++)
        {
            RelicChest chest = activeChests[index];
            if (chest == null || chest.IsOpened || chest.gameObject.activeInHierarchy == false)
                continue;

            if (TryGetChestMarkerPlacement(chest, out Room chestRoom,
                    out Vector2 normalizedPosition) == false)
                continue;

            if (ReferenceEquals(chestRoom, _currentRoom) == false)
                continue;

            currentIcon.SetChestPosition(normalizedPosition);
        }
    }

    public void Dispose()
    {
        _relicEventBus.ChestsCleared -= HandleChestsCleared;
        _relicEventBus.ChestSpawned -= HandleChestChanged;
        _relicEventBus.ChestCollected -= HandleChestCollected;
    }

    private void HandleChestsCleared() =>
        Clear();

    private void HandleChestChanged(RoomData roomData, Room room, Vector3 worldPosition) =>
        Refresh();

    private void HandleChestCollected(RoomData roomData, Room room) =>
        Refresh();

    private bool TryGetChestMarkerPlacement(RelicChest chest, out Room room,
        out Vector2 normalizedPosition)
    {
        room = null;
        normalizedPosition = Vector2.zero;

        if (TryFindRoomAtWorldPosition(chest.transform.position, out room) == false)
            room = chest.Room;

        if (room == null || _iconsByRoom.ContainsKey(room) == false)
            return false;

        Vector3 localPosition = room.transform.InverseTransformPoint(chest.transform.position);
        MinimapRoomBounds bounds = _boundsByRoom.TryGetValue(room, out MinimapRoomBounds roomBounds)
            ? roomBounds
            : MinimapRoomBounds.Default;
        normalizedPosition = bounds.Normalize(localPosition);
        return true;
    }

    private bool TryFindRoomAtWorldPosition(Vector3 worldPosition, out Room resolvedRoom)
    {
        resolvedRoom = null;
        float closestDistanceSqr = float.PositiveInfinity;

        foreach (Room room in _iconsByRoom.Keys)
        {
            if (room == null)
                continue;

            Vector3 localPosition = room.transform.InverseTransformPoint(worldPosition);
            MinimapRoomBounds bounds = _boundsByRoom.TryGetValue(room, out MinimapRoomBounds roomBounds)
                ? roomBounds
                : MinimapRoomBounds.Default;

            if (bounds.Contains(localPosition, 1f) == false)
                continue;

            float distanceSqr = (room.transform.position - worldPosition).sqrMagnitude;
            if (distanceSqr >= closestDistanceSqr)
                continue;

            closestDistanceSqr = distanceSqr;
            resolvedRoom = room;
        }

        if (resolvedRoom != null)
            return true;

        foreach (Room room in _iconsByRoom.Keys)
        {
            if (room == null)
                continue;

            float distanceSqr = (room.transform.position - worldPosition).sqrMagnitude;
            if (distanceSqr >= closestDistanceSqr)
                continue;

            closestDistanceSqr = distanceSqr;
            resolvedRoom = room;
        }

        return resolvedRoom != null;
    }
}
