using System;
using UnityEngine;

public enum RoomDirection
{
    Up,
    Down,
    Left,
    Right
}

public static class RoomDirectionExtensions
{
    public static Vector2Int ToGridOffset(this RoomDirection direction) =>
        direction switch
        {
            RoomDirection.Up => Vector2Int.up,
            RoomDirection.Down => Vector2Int.down,
            RoomDirection.Left => Vector2Int.left,
            RoomDirection.Right => Vector2Int.right,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };

    public static RoomDirection Opposite(this RoomDirection direction) =>
        direction switch
        {
            RoomDirection.Up => RoomDirection.Down,
            RoomDirection.Down => RoomDirection.Up,
            RoomDirection.Left => RoomDirection.Right,
            RoomDirection.Right => RoomDirection.Left,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
}
