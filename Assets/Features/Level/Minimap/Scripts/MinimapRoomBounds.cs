using UnityEngine;

public readonly struct MinimapRoomBounds
{
    public static MinimapRoomBounds Default { get; } = new(
        -LevelView.RoomWorldSize * 0.5f,
        LevelView.RoomWorldSize * 0.5f,
        -LevelView.RoomWorldSize * 0.5f,
        LevelView.RoomWorldSize * 0.5f);

    private readonly float _minX;
    private readonly float _maxX;
    private readonly float _minZ;
    private readonly float _maxZ;

    public MinimapRoomBounds(float minX, float maxX, float minZ, float maxZ)
    {
        _minX = minX;
        _maxX = maxX;
        _minZ = minZ;
        _maxZ = maxZ;
    }

    public Vector2 Normalize(Vector3 localPosition) =>
        new(
            Mathf.InverseLerp(_minX, _maxX, localPosition.x) * 2f - 1f,
            Mathf.InverseLerp(_minZ, _maxZ, localPosition.z) * 2f - 1f);

    public bool Contains(Vector3 localPosition, float padding) =>
        localPosition.x >= _minX - padding &&
        localPosition.x <= _maxX + padding &&
        localPosition.z >= _minZ - padding &&
        localPosition.z <= _maxZ + padding;
}
