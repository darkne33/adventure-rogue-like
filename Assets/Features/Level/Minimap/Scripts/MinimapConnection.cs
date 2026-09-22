using UnityEngine;
using UnityEngine.UI;

public sealed class MinimapConnection : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private Color _visibleColor = new(0.43f, 0.40f, 0.34f, 1f);
    [SerializeField] private Color _highlightedColor = new(0.91f, 0.83f, 0.60f, 1f);

    public void Configure(Image image) =>
        _image = image;

    public void SetState(bool isVisible, bool isHighlighted)
    {
        gameObject.SetActive(isVisible);
        if (isVisible)
            _image.color = isHighlighted ? _highlightedColor : _visibleColor;
    }
}
