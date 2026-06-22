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
    private readonly HashSet<RoomData> _visitedRooms = new();
    private Room _currentRoom;

    public MinimapChestMarkerController(RelicChestSpawner relicChestSpawner,
        RelicEventBus relicEventBus)
    {
        _relicChestSpawner = relicChestSpawner;
        _relicEventBus = relicEventBus;
        _relicEventBus.ChestsCleared += HandleChestsCleared;
        _relicEventBus.ChestSpawned += HandleChestChanged;
        _relicEventBus.ChestCollected += HandleChestCollected;
        _relicEventBus.RoomStarted += HandleRoomStarted;
    }

    public void SetRooms(IReadOnlyDictionary<Room, MinimapRoomIcon> iconsByRoom,
        IReadOnlyDictionary<Room, MinimapRoomBounds> boundsByRoom)
    {
        _iconsByRoom = iconsByRoom ?? new Dictionary<Room, MinimapRoomIcon>();
        Refresh();
    }

    public void SetCurrentRoom(Room currentRoom)
    {
        _currentRoom = currentRoom;
        Refresh();
    }

    public void Clear()
        => ClearMarkers();

    private void ClearMarkers()
    {
        foreach (MinimapRoomIcon icon in _iconsByRoom.Values)
        {
            if (icon != null)
                icon.SetChestVisible(false);
        }
    }

    public void Refresh()
    {
        ClearMarkers();

        IReadOnlyList<RelicChest> activeChests = _relicChestSpawner.ActiveChests;
        for (int index = 0; index < activeChests.Count; index++)
        {
            RelicChest chest = activeChests[index];
            if (chest == null || chest.IsOpened || chest.gameObject.activeInHierarchy == false)
                continue;

            Room chestRoom = chest.Room;
            RoomData chestRoomData = chest.RoomData;
            if (chestRoom == null || chestRoomData == null ||
                _iconsByRoom.TryGetValue(chestRoom, out MinimapRoomIcon icon) == false)
                continue;

            if (ReferenceEquals(chestRoom, _currentRoom) || _visitedRooms.Contains(chestRoomData))
                continue;

            icon.SetChestVisible(true);
        }
    }

    public void Dispose()
    {
        _relicEventBus.ChestsCleared -= HandleChestsCleared;
        _relicEventBus.ChestSpawned -= HandleChestChanged;
        _relicEventBus.ChestCollected -= HandleChestCollected;
        _relicEventBus.RoomStarted -= HandleRoomStarted;
    }

    private void HandleChestsCleared()
    {
        _visitedRooms.Clear();
        ClearMarkers();
    }

    private void HandleChestChanged(RoomData roomData, Room room, Vector3 worldPosition) =>
        Refresh();

    private void HandleChestCollected(RoomData roomData, Room room) =>
        Refresh();

    private void HandleRoomStarted(RelicRoomEvent roomEvent)
    {
        if (roomEvent.RoomData != null)
            _visitedRooms.Add(roomEvent.RoomData);

        Refresh();
    }
}
