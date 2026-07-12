using UnityEngine;

[DisallowMultipleComponent]
public sealed class DoorAnimator : MonoBehaviour
{
    [SerializeField] private DoorView[] _doors;

    public bool IsConfigured
    {
        get
        {
            if (_doors == null || _doors.Length == 0)
                return false;

            bool hasEnemyDoor = false;
            bool hasRewardDoor = false;

            foreach (DoorView door in _doors)
            {
                if (door == null || !door.IsConfigured)
                    return false;

                if (door.Type == DoorType.Enemy)
                    hasEnemyDoor = true;
                else if (door.Type == DoorType.Reward)
                    hasRewardDoor = true;
            }

            return hasEnemyDoor && hasRewardDoor;
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
                $"{name} must contain configured Enemy and Reward DoorView references.");
    }
}

