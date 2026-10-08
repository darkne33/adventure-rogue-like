using UnityEngine;

[DisallowMultipleComponent]
public sealed class DoorAnimator : MonoBehaviour
{
    private const int RequiredTypes = (1 << (int)DoorType.Enemy) | (1 << (int)DoorType.Reward) |
                                      (1 << (int)DoorType.Item) | (1 << (int)DoorType.Shop) |
                                      (1 << (int)DoorType.Boss) | (1 << (int)DoorType.Blood);

    [SerializeField] private DoorView[] _doors;

    public bool IsConfigured
    {
        get
        {
            if (_doors == null || _doors.Length == 0)
                return false;

            int configuredTypes = 0;

            foreach (DoorView door in _doors)
            {
                if (door == null || !door.IsConfigured)
                    return false;

                configuredTypes |= 1 << (int)door.Type;
            }

            return (configuredTypes & RequiredTypes) == RequiredTypes;
        }
    }

    public void Hide()
    {
        EnsureConfigured();

        foreach (DoorView door in _doors)
            door.Hide();
    }

    public void Open(DoorType type) =>
        Show(type, isOpen: true);

    public void Close(DoorType type) =>
        Show(type, isOpen: false);

    public void SetHighlight(bool isHighlighted, Color color)
    {
        EnsureConfigured();

        foreach (DoorView door in _doors)
            door.SetHighlight(isHighlighted && door.gameObject.activeSelf, color);
    }

    private void Show(DoorType type, bool isOpen)
    {
        EnsureConfigured();

        foreach (DoorView door in _doors)
        {
            if (door.Type == type)
                door.Show(isOpen);
            else
                door.Hide();
        }
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
            throw new MissingReferenceException(
                $"{name} must contain configured Enemy, Treasure, Item, Shop, Boss and Blood DoorView references.");
    }
}
