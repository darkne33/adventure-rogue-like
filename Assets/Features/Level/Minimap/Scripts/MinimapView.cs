using UnityEngine;

public sealed class MinimapView : MonoBehaviour
{
    [SerializeField] private RectTransform _content;
    [SerializeField] private MinimapRoomIcon _roomIconPrefab;
    [SerializeField] private MinimapConnection _horizontalConnectionPrefab;
    [SerializeField] private MinimapConnection _verticalConnectionPrefab;
    [SerializeField, Min(1f)] private float _cellSize = 116f;

    public RectTransform Content => _content;
    public MinimapRoomIcon RoomIconPrefab => _roomIconPrefab;
    public MinimapConnection HorizontalConnectionPrefab => _horizontalConnectionPrefab;
    public MinimapConnection VerticalConnectionPrefab => _verticalConnectionPrefab;
    public float CellSize => _cellSize;

    public void Configure(RectTransform content, MinimapRoomIcon roomIconPrefab,
        MinimapConnection horizontalConnectionPrefab,
        MinimapConnection verticalConnectionPrefab, float cellSize)
    {
        _content = content;
        _roomIconPrefab = roomIconPrefab;
        _horizontalConnectionPrefab = horizontalConnectionPrefab;
        _verticalConnectionPrefab = verticalConnectionPrefab;
        _cellSize = cellSize;
    }

    public void Clear()
    {
        for (int index = _content.childCount - 1; index >= 0; index--)
            Destroy(_content.GetChild(index).gameObject);
    }
}
