using System;
using UnityEngine;

public enum RoomDirection
{
    Up,
    Down,
    Left,
    Right
}

[Flags]
public enum RoomConnectionMask
{
    None = 0,
    Up = 1,
    Down = 2,
    Left = 4,
    Right = 8,
    All = 15
}

public static class RoomDirectionExtensions
{
    public static RoomConnectionMask ToConnectionMask(this RoomDirection direction) =>
        direction switch
        {
            RoomDirection.Up => RoomConnectionMask.Up,
            RoomDirection.Down => RoomConnectionMask.Down,
            RoomDirection.Left => RoomConnectionMask.Left,
            RoomDirection.Right => RoomConnectionMask.Right,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };

    public static RoomDirection RotateClockwise(this RoomDirection direction, int quarterTurns)
    {
        int directionIndex = direction switch
        {
            RoomDirection.Up => 0,
            RoomDirection.Right => 1,
            RoomDirection.Down => 2,
            RoomDirection.Left => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
        int normalizedQuarterTurns = ((quarterTurns % 4) + 4) % 4;

        return ((directionIndex + normalizedQuarterTurns) % 4) switch
        {
            0 => RoomDirection.Up,
            1 => RoomDirection.Right,
            2 => RoomDirection.Down,
            _ => RoomDirection.Left
        };
    }

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
