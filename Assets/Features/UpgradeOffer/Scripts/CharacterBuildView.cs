using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CharacterBuildView : MonoBehaviour
{
    [SerializeField] private CharacterBuildSlotView[] _weaponSlots = Array.Empty<CharacterBuildSlotView>();
    [SerializeField] private CharacterBuildSlotView[] _scrollSlots = Array.Empty<CharacterBuildSlotView>();

    private void Awake() =>
        Refresh(null);

    public void Refresh(IReadOnlyList<UpgradeBuildEntry> upgrades)
    {
        RefreshColumn<CharacterActiveAbility>(upgrades, _weaponSlots);
        RefreshColumn<CharacterPassiveAbility>(upgrades, _scrollSlots);
    }

    private static void RefreshColumn<TAbility>(IReadOnlyList<UpgradeBuildEntry> upgrades,
        CharacterBuildSlotView[] slots) where TAbility : CharacterAbility
    {
        int slotIndex = 0;

        if (upgrades != null)
        {
            foreach (UpgradeBuildEntry upgrade in upgrades)
            {
                if (slotIndex >= slots.Length)
                    break;

                if (upgrade?.Ability is TAbility)
                    slots[slotIndex++].SetUpgrade(upgrade);
            }
        }

        while (slotIndex < slots.Length)
            slots[slotIndex++].SetEmpty();
    }
}
