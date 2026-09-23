using UnityEngine;

[DisallowMultipleComponent]
public sealed class DoorView : MonoBehaviour
{
    [SerializeField] private DoorType _type;
    [SerializeField] private GameObject _leftLeaf;
    [SerializeField] private GameObject _rightLeaf;
    [SerializeField] private Outline _outline;

    public DoorType Type => _type;
    public bool IsConfigured => _leftLeaf != null && _rightLeaf != null && _outline != null;

    public void Show(bool isOpen)
    {
        EnsureConfigured();
        gameObject.SetActive(true);
        _leftLeaf.SetActive(!isOpen);
        _rightLeaf.SetActive(!isOpen);
    }

    public void Hide()
    {
        EnsureConfigured();
        _outline.enabled = false;
        gameObject.SetActive(false);
    }

    public void SetHighlight(bool isHighlighted, Color color)
    {
        EnsureConfigured();
        _outline.OutlineColor = color;
        _outline.enabled = isHighlighted;
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
            throw new MissingReferenceException(
                $"{name} must contain assigned left and right door leaves and an outline.");
    }
}
