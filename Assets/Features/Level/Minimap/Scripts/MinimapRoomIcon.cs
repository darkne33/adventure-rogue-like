using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class MinimapRoomIcon : MonoBehaviour
{
    [SerializeField] private Image _fill;
    [SerializeField] private Sprite _availableSprite;
    [SerializeField] private Sprite _visitedSprite;
    [SerializeField] private Sprite _currentSprite;
    [SerializeField] private Image _outline;
    [SerializeField] private Image _startMarker;
    [SerializeField] private Image _exitMarker;
    [SerializeField] private Image _shopMarker;
    [SerializeField] private UnityEngine.UI.Image _bossMarker;
    [SerializeField] private UnityEngine.UI.Image _relicRoomMarker;
    [SerializeField] private UnityEngine.UI.Image _bloodRoomMarker;
    [SerializeField] private Image _chestMarker;
    [SerializeField] private Image _combatRoomMarker;
    [SerializeField] private RectTransform _playerMarker;
    [SerializeField] private bool _showPositionMarkers = true;
    [SerializeField] private RectTransform _roomDirectionMarker;
    [SerializeField, Min(0f)] private float _roomDirectionGap = 2f;
    [SerializeField] private RectTransform _enemyMarkerRoot;
    [SerializeField] private RectTransform _enemyMarkerPrefab;
    [SerializeField] private RectTransform _goldMarkerRoot;
    [SerializeField] private RectTransform _goldMarkerPrefab;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Color _availableOutlineColor =
        new(0.02f, 0.02f, 0.02f, 1f);
    [SerializeField] private Color _visitedOutlineColor =
        new(0.02f, 0.02f, 0.02f, 1f);
    [SerializeField] private Color _currentOutlineColor =
        new(0.02f, 0.02f, 0.02f, 1f);
    [SerializeField] private Color _bossOutlineColor =
        new(0.18f, 0.05f, 0.04f, 1f);
    [SerializeField] private Color _availableFillColor =
        new(42f / 255f, 41f / 255f, 35f / 255f, 1f);
    [SerializeField] private Color _visitedFillColor =
        new(100f / 255f, 100f / 255f, 100f / 255f, 1f);
    [SerializeField] private Color _currentFillColor =
        new(212f / 255f, 212f / 255f, 212f / 255f, 1f);
    [SerializeField] private Vector2 _playerMarkerRange = new(18f, 18f);
    [SerializeField] private Vector2 _enemyMarkerRange = new(18f, 18f);
    [SerializeField] private Vector2 _goldMarkerRange = new(18f, 18f);
    [SerializeField] private Vector2 _chestMarkerRange = new(18f, 18f);
    [SerializeField, Min(0f)] private float _exitMarkerDistance = 38f;

    private readonly List<RectTransform> _enemyMarkers = new();
    private readonly List<RectTransform> _goldMarkers = new();
    private MinimapRoomKind _kind;
    private RoomDirection? _exitDirection;
    private bool _isRoomKindMarkerVisible = true;

    public void Configure(Image fill, Image outline, Image startMarker,
        Image exitMarker, Image chestMarker, RectTransform playerMarker, CanvasGroup canvasGroup,
        Image shopMarker = null)
    {
        _fill = fill;
        _outline = outline;
        _startMarker = startMarker;
        _exitMarker = exitMarker;
        _shopMarker = shopMarker;
        _chestMarker = chestMarker;
        _playerMarker = playerMarker;
        _canvasGroup = canvasGroup;
    }

    public void SetKind(MinimapRoomKind kind, RoomDirection? exitDirection)
    {
        _kind = kind;
        _exitDirection = exitDirection;

        if (_kind == MinimapRoomKind.Exit && _exitDirection.HasValue)
            _exitMarker.rectTransform.anchoredPosition =
                GetDirection(_exitDirection.Value) * _exitMarkerDistance;

        RefreshRoomKindMarkers();
    }

    public void SetRoomKindMarkerVisible(bool isVisible)
    {
        _isRoomKindMarkerVisible = isVisible;
        RefreshRoomKindMarkers();
    }

    public void SetChestVisible(bool isVisible)
    {
        if (isVisible && _chestMarker != null)
            _chestMarker.rectTransform.anchoredPosition = Vector2.zero;

        if (_chestMarker != null)
            _chestMarker.gameObject.SetActive(isVisible);
    }

    public void SetCombatRoomMarkerVisible(bool isVisible)
    {
        if (_combatRoomMarker != null)
            _combatRoomMarker.gameObject.SetActive(isVisible);
    }

    public void SetRoomMarkerDirection(RoomDirection direction)
    {
        float zRotation = direction switch
        {
            RoomDirection.Up => 180f,
            RoomDirection.Down => 0f,
            RoomDirection.Left => -90f,
            RoomDirection.Right => 90f,
            _ => 0f
        };

        SetMarkerRotation(_startMarker, zRotation);
        SetMarkerRotation(_exitMarker, zRotation);
        SetMarkerRotation(_shopMarker, zRotation);
        SetMarkerRotation(_bossMarker, zRotation);
        SetMarkerRotation(_relicRoomMarker, zRotation);
        SetMarkerRotation(_bloodRoomMarker, zRotation);
        SetMarkerRotation(_chestMarker, zRotation);
        SetMarkerRotation(_combatRoomMarker, zRotation);
    }

    public void SetChestPosition(Vector2 normalizedPosition)
    {
        if (_chestMarker == null)
            return;

        normalizedPosition.x = Mathf.Clamp(normalizedPosition.x, -1f, 1f);
        normalizedPosition.y = Mathf.Clamp(normalizedPosition.y, -1f, 1f);
        _chestMarker.rectTransform.anchoredPosition = normalizedPosition * _chestMarkerRange;
        _chestMarker.gameObject.SetActive(true);
    }

    public void SetState(MinimapRoomState state)
    {
        bool isHidden = state == MinimapRoomState.Hidden;
        _canvasGroup.alpha = isHidden ? 0f : 1f;
        _playerMarker.gameObject.SetActive(_showPositionMarkers && state == MinimapRoomState.Current);
        if (_roomDirectionMarker != null)
        {
            _roomDirectionMarker.gameObject.SetActive(state == MinimapRoomState.Current);
            if (state == MinimapRoomState.Current)
                transform.SetAsLastSibling();
        }
        if (state != MinimapRoomState.Current)
            SetEnemyPositions(null);

        _outline.color = state switch
        {
            MinimapRoomState.Available => _availableOutlineColor,
            MinimapRoomState.Visited => _visitedOutlineColor,
            MinimapRoomState.Current => _currentOutlineColor,
            _ => Color.clear
        };

        if (_kind == MinimapRoomKind.Boss && !isHidden)
        {
            Color bossColor = _bossOutlineColor;
            bossColor.a *= _outline.color.a;
            _outline.color = bossColor;
        }

        Sprite fillSprite = state switch
        {
            MinimapRoomState.Available => _availableSprite,
            MinimapRoomState.Visited => _visitedSprite,
            MinimapRoomState.Current => _currentSprite,
            _ => _availableSprite
        };

        if (fillSprite != null)
        {
            _fill.sprite = fillSprite;
            _fill.color = isHidden ? Color.clear : Color.white;
        }
        else
        {
            _fill.color = state switch
            {
                MinimapRoomState.Available => _availableFillColor,
                MinimapRoomState.Visited => _visitedFillColor,
                MinimapRoomState.Current => _currentFillColor,
                _ => Color.clear
            };
        }
    }

    public void SetPlayerPosition(Vector2 normalizedPosition)
    {
        normalizedPosition.x = Mathf.Clamp(normalizedPosition.x, -1f, 1f);
        normalizedPosition.y = Mathf.Clamp(normalizedPosition.y, -1f, 1f);
        _playerMarker.anchoredPosition = normalizedPosition * _playerMarkerRange;
    }

    public void SetPlayerRotation(float zRotation) =>
        _playerMarker.localRotation = Quaternion.Euler(0f, 0f, zRotation);

    public void SetRoomDirection(float cameraYaw)
    {
        if (_roomDirectionMarker == null)
            return;

        float radians = cameraYaw * Mathf.Deg2Rad;
        Vector2 direction = new(Mathf.Sin(radians), Mathf.Cos(radians));
        Vector2 roomSize = _fill.rectTransform.rect.size;
        float radius = Mathf.Max(roomSize.x, roomSize.y) * 0.5f + _roomDirectionGap;

        // The map already rotates by +yaw. Apply the camera heading once in room space.
        _roomDirectionMarker.anchoredPosition = direction * radius;
        _roomDirectionMarker.localRotation = Quaternion.Euler(0f, 0f, -cameraYaw);
    }

    public void SetEnemyPositions(IReadOnlyList<Vector2> normalizedPositions)
    {
        int count = _showPositionMarkers ? (normalizedPositions?.Count ?? 0) : 0;
        EnsureEnemyMarkers(count);

        for (int index = 0; index < _enemyMarkers.Count; index++)
        {
            RectTransform marker = _enemyMarkers[index];
            bool isVisible = index < count;
            marker.gameObject.SetActive(isVisible);

            if (!isVisible)
                continue;

            Vector2 normalizedPosition = normalizedPositions[index];
            normalizedPosition.x = Mathf.Clamp(normalizedPosition.x, -1f, 1f);
            normalizedPosition.y = Mathf.Clamp(normalizedPosition.y, -1f, 1f);
            marker.anchoredPosition = normalizedPosition * _enemyMarkerRange;
        }
    }

    public void SetGoldPositions(IReadOnlyList<Vector2> normalizedPositions)
    {
        int count = _showPositionMarkers ? (normalizedPositions?.Count ?? 0) : 0;
        EnsureMarkers(_goldMarkers, _goldMarkerRoot, _goldMarkerPrefab, count);

        for (int index = 0; index < _goldMarkers.Count; index++)
        {
            RectTransform marker = _goldMarkers[index];
            bool isVisible = index < count;
            marker.gameObject.SetActive(isVisible);

            if (!isVisible)
                continue;

            Vector2 normalizedPosition = normalizedPositions[index];
            normalizedPosition.x = Mathf.Clamp(normalizedPosition.x, -1f, 1f);
            normalizedPosition.y = Mathf.Clamp(normalizedPosition.y, -1f, 1f);
            marker.anchoredPosition = normalizedPosition * _goldMarkerRange;
        }
    }

    private void EnsureEnemyMarkers(int count)
    {
        EnsureMarkers(_enemyMarkers, _enemyMarkerRoot, _enemyMarkerPrefab, count);
    }

    private static void EnsureMarkers(List<RectTransform> markers, RectTransform markerRoot,
        RectTransform markerPrefab, int count)
    {
        if (markerRoot == null || markerPrefab == null)
            return;

        while (markers.Count < count)
        {
            RectTransform marker = Instantiate(markerPrefab, markerRoot);
            marker.gameObject.SetActive(false);
            markers.Add(marker);
        }
    }

    private static void SetMarkerRotation(Image marker, float zRotation)
    {
        if (marker != null)
            marker.rectTransform.localRotation = Quaternion.Euler(0f, 0f, zRotation);
    }

    private void RefreshRoomKindMarkers()
    {
        if (_startMarker != null)
            _startMarker.gameObject.SetActive(
                _isRoomKindMarkerVisible && _kind == MinimapRoomKind.Start);

        if (_exitMarker != null)
            _exitMarker.gameObject.SetActive(
                _isRoomKindMarkerVisible && _kind == MinimapRoomKind.Exit);

        if (_shopMarker != null)
            _shopMarker.gameObject.SetActive(
                _isRoomKindMarkerVisible && _kind == MinimapRoomKind.Shop);

        if (_bossMarker != null)
            _bossMarker.gameObject.SetActive(
                _isRoomKindMarkerVisible && _kind == MinimapRoomKind.Boss);

        if (_relicRoomMarker != null)
            _relicRoomMarker.gameObject.SetActive(
                _isRoomKindMarkerVisible && _kind == MinimapRoomKind.OnlyRelic);

        if (_bloodRoomMarker != null)
            _bloodRoomMarker.gameObject.SetActive(
                _isRoomKindMarkerVisible && _kind == MinimapRoomKind.Blood);
    }

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
