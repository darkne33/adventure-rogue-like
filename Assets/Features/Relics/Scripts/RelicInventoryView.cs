using System.Collections.Generic;
using UnityEngine;

namespace Features.Relics.Scripts
{
    public sealed class RelicInventoryView : MonoBehaviour
    {
        [SerializeField] private RectTransform _slotsRoot;
        [SerializeField] private RelicInventorySlotView _slotPrefab;
        [SerializeField] private RelicTooltipView _tooltipView;
        [SerializeField] private Vector2 _slotSize = new(48f, 48f);
        [SerializeField] private float _slotSpacing = 6f;

        private readonly List<RelicInventorySlotView> _slots = new();

        public void Refresh(IReadOnlyList<RelicRuntimeState> relics)
        {
            ClearSlots();

            if (_slotsRoot == null || _slotPrefab == null)
                return;

            for (int index = 0; index < relics.Count; index++)
            {
                RelicInventorySlotView slot = Instantiate(_slotPrefab, _slotsRoot);
                RelicRuntimeState state = relics[index];
                slot.name = $"RelicSlot_{state.Definition.Id}";
                RectTransform slotTransform = slot.transform as RectTransform;
                if (slotTransform != null)
                {
                    slotTransform.anchorMin = new Vector2(0f, 1f);
                    slotTransform.anchorMax = new Vector2(0f, 1f);
                    slotTransform.pivot = new Vector2(0f, 1f);
                    slotTransform.sizeDelta = _slotSize;
                    slotTransform.anchoredPosition = new Vector2(index * (_slotSize.x + _slotSpacing), 0f);
                }

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
            RelicTooltipView tooltipView)
        {
            _slotsRoot = slotsRoot;
            _slotPrefab = slotPrefab;
            _tooltipView = tooltipView;
        }
#endif

        private void Reset()
        {
            _slotsRoot = transform as RectTransform;
            _tooltipView = GetComponentInChildren<RelicTooltipView>(true);
        }
    }
}
