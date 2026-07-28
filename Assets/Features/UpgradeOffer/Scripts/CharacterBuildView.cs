using System.Collections.Generic;
using UnityEngine;

public sealed class CharacterBuildView : MonoBehaviour
{
    [SerializeField] private CharacterBuildSlotView _slotPrefab;
    [SerializeField, Min(1)] private int _slotCount = 5;

    private readonly List<CharacterBuildSlotView> _slots = new();

    private void Awake()
    {
        EnsureSlots();
        Refresh(null);
    }

    public void Refresh(IReadOnlyList<UpgradeBuildEntry> upgrades)
    {
        EnsureSlots();

        for (int index = 0; index < _slots.Count; index++)
        {
            if (upgrades != null && index < upgrades.Count)
                _slots[index].SetUpgrade(upgrades[index]);
            else
                _slots[index].SetEmpty();
        }
    }

    private void EnsureSlots()
    {
        if (_slotPrefab == null)
            return;

        while (_slots.Count < _slotCount)
        {
            CharacterBuildSlotView slot = Instantiate(_slotPrefab, transform);
            slot.name = $"BuildSlot_{_slots.Count + 1}";
            _slots.Add(slot);
        }
    }
}
