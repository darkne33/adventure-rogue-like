using UnityEngine;
using Zenject;

public sealed class MinimapElementFactory
{
    private readonly DiContainer _container;

    public MinimapElementFactory(DiContainer container) =>
        _container = container;

    public MinimapRoomIcon CreateRoom(MinimapView view, Vector2 position)
    {
        MinimapRoomIcon icon = _container.InstantiatePrefabForComponent<MinimapRoomIcon>(
            view.RoomIconPrefab, view.Content);
        icon.GetComponent<RectTransform>().anchoredPosition = position;
        return icon;
    }

    public MinimapConnection CreateConnection(MinimapView view, Vector2 position,
        bool isHorizontal)
    {
        MinimapConnection prefab = isHorizontal
            ? view.HorizontalConnectionPrefab
            : view.VerticalConnectionPrefab;
        MinimapConnection connection =
            _container.InstantiatePrefabForComponent<MinimapConnection>(prefab, view.Content);

        RectTransform rectTransform = connection.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.SetAsFirstSibling();
        return connection;
    }
}
