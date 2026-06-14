using UnityEngine;
using UnityEngine.UI;

public sealed class MinimapConnection : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private Color _visibleColor = new(0.82f, 0.86f, 0.88f, 0.48f);
    [SerializeField] private Color _highlightedColor = new(1f, 1f, 1f, 0.92f);

    public void Configure(Image image) =>
        _image = image;

    public void SetState(bool isVisible, bool isHighlighted)
    {
        gameObject.SetActive(isVisible);
        if (isVisible)
            _image.color = isHighlighted ? _highlightedColor : _visibleColor;
    }
}
