using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Relics.Scripts
{
    public sealed class RelicInventoryView : MonoBehaviour
    {
        [SerializeField] private RectTransform _slotsRoot;
        [SerializeField] private RelicInventorySlotView _slotPrefab;
        [SerializeField] private RelicTooltipView _tooltipView;
        [SerializeField] private GridLayoutGroup _gridLayoutGroup;

        private readonly List<RelicInventorySlotView> _slots = new();

        public void Refresh(IReadOnlyList<RelicRuntimeState> relics)
        {
            ClearSlots();

            if (_slotsRoot == null || _slotPrefab == null)
                return;

            _gridLayoutGroup ??= _slotsRoot.GetComponent<GridLayoutGroup>();

            for (int index = 0; index < relics.Count; index++)
            {
                RelicInventorySlotView slot = Instantiate(_slotPrefab, _slotsRoot);
                RelicRuntimeState state = relics[index];
                slot.name = $"RelicSlot_{state.Definition.Id}";

                slot.Construct(state, _tooltipView);
                _slots.Add(slot);
            }
        }

        public void HideTooltip() =>
            _tooltipView?.Hide();

        public void ClearSlots()
        {
            foreach (RelicInventorySlotView slot in _slots)
            {
                if (slot != null)
                    Destroy(slot.gameObject);
            }

            _slots.Clear();
            HideTooltip();
        }

#if UNITY_EDITOR
        public void SetEditorReferences(RectTransform slotsRoot, RelicInventorySlotView slotPrefab,
            RelicTooltipView tooltipView, GridLayoutGroup gridLayoutGroup)
        {
            _slotsRoot = slotsRoot;
            _slotPrefab = slotPrefab;
            _tooltipView = tooltipView;
            _gridLayoutGroup = gridLayoutGroup;
        }
#endif

        private void Reset()
        {
            _slotsRoot = transform as RectTransform;
            _tooltipView = GetComponentInChildren<RelicTooltipView>(true);
            _gridLayoutGroup = GetComponentInChildren<GridLayoutGroup>(true);
        }
    }
}
