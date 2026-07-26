using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterCrosshairView : MonoBehaviour
{
    [SerializeField] private Graphic[] _graphics;
    [SerializeField] private Color _normalColor = new(1f, 1f, 1f, 0.92f);
    [SerializeField] private Color _targetColor = new(1f, 0.2f, 0.2f, 1f);

    private bool _isInitialized;
    private bool _isTargeted;

    private void Awake()
    {
        _isInitialized = true;
        ApplyColor(_normalColor);
    }

    public void SetTargeted(bool state)
    {
        if (_isInitialized && _isTargeted == state)
            return;

        _isInitialized = true;
        _isTargeted = state;
        ApplyColor(state ? _targetColor : _normalColor);
    }

    private void ApplyColor(Color color)
    {
        if (_graphics == null)
            return;

        foreach (Graphic graphic in _graphics)
        {
            if (graphic != null)
                graphic.color = color;
        }
    }
}
