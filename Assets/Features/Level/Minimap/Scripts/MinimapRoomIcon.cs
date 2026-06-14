using UnityEngine;
using UnityEngine.UI;

public sealed class MinimapRoomIcon : MonoBehaviour
{
    [SerializeField] private Image _fill;
    [SerializeField] private Image _outline;
    [SerializeField] private Image _startMarker;
    [SerializeField] private Image _exitMarker;
    [SerializeField] private RectTransform _playerMarker;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Color _availableOutlineColor =
        new(1f, 1f, 1f, 0.34f);
    [SerializeField] private Color _visitedOutlineColor =
        new(1f, 1f, 1f, 0.68f);
    [SerializeField] private Color _currentOutlineColor = Color.white;
    [SerializeField] private Color _availableFillColor =
        new(0.02f, 0.02f, 0.02f, 0.04f);
    [SerializeField] private Color _visitedFillColor =
        new(0.02f, 0.02f, 0.02f, 0.12f);
    [SerializeField] private Color _currentFillColor =
        new(0.02f, 0.02f, 0.02f, 0.22f);
    [SerializeField] private Vector2 _playerMarkerRange = new(18f, 18f);
    [SerializeField, Min(0f)] private float _exitMarkerDistance = 38f;

    public void Configure(Image fill, Image outline, Image startMarker,
        Image exitMarker, RectTransform playerMarker, CanvasGroup canvasGroup)
    {
        _fill = fill;
        _outline = outline;
        _startMarker = startMarker;
        _exitMarker = exitMarker;
        _playerMarker = playerMarker;
        _canvasGroup = canvasGroup;
    }

    public void SetKind(MinimapRoomKind kind, RoomDirection? exitDirection)
    {
        _startMarker.gameObject.SetActive(kind == MinimapRoomKind.Start);
        _exitMarker.gameObject.SetActive(kind == MinimapRoomKind.Exit);

        if (kind == MinimapRoomKind.Exit && exitDirection.HasValue)
            _exitMarker.rectTransform.anchoredPosition =
                GetDirection(exitDirection.Value) * _exitMarkerDistance;
    }

    public void SetState(MinimapRoomState state)
    {
        bool isHidden = state == MinimapRoomState.Hidden;
        _canvasGroup.alpha = isHidden ? 0f : 1f;
        _playerMarker.gameObject.SetActive(state == MinimapRoomState.Current);

        _outline.color = state switch
        {
            MinimapRoomState.Available => _availableOutlineColor,
            MinimapRoomState.Visited => _visitedOutlineColor,
            MinimapRoomState.Current => _currentOutlineColor,
            _ => Color.clear
        };

        _fill.color = state switch
        {
            MinimapRoomState.Available => _availableFillColor,
            MinimapRoomState.Visited => _visitedFillColor,
            MinimapRoomState.Current => _currentFillColor,
            _ => Color.clear
        };
    }

    public void SetPlayerPosition(Vector2 normalizedPosition)
    {
        normalizedPosition.x = Mathf.Clamp(normalizedPosition.x, -1f, 1f);
        normalizedPosition.y = Mathf.Clamp(normalizedPosition.y, -1f, 1f);
        _playerMarker.anchoredPosition = normalizedPosition * _playerMarkerRange;
    }

    public void SetPlayerRotation(float zRotation) =>
        _playerMarker.localRotation = Quaternion.Euler(0f, 0f, zRotation);

    private static Vector2 GetDirection(RoomDirection direction) =>
        direction switch
        {
            RoomDirection.Up => Vector2.up,
            RoomDirection.Down => Vector2.down,
            RoomDirection.Left => Vector2.left,
            RoomDirection.Right => Vector2.right,
            _ => Vector2.zero
        };
}
