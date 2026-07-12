using UnityEngine;

[DisallowMultipleComponent]
public sealed class DoorView : MonoBehaviour
{
    [SerializeField] private DoorType _type;
    [SerializeField] private GameObject _leftLeaf;
    [SerializeField] private GameObject _rightLeaf;

    public DoorType Type => _type;
    public bool IsConfigured => _leftLeaf != null && _rightLeaf != null;

    public void Show(bool isOpen)
    {
        EnsureConfigured();
        gameObject.SetActive(true);
        _leftLeaf.SetActive(!isOpen);
        _rightLeaf.SetActive(!isOpen);
    }

    public void Hide() =>
        gameObject.SetActive(false);

    private void EnsureConfigured()
    {
        if (!IsConfigured)
            throw new MissingReferenceException(
                $"{name} must contain assigned left and right door leaves.");
    }
}
