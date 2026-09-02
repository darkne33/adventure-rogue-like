using System.Collections.Generic;
using Features.Relics.Scripts;
using UnityEngine;
using UnityEngine.UI;

public sealed class PauseRelicInventoryView : MonoBehaviour
{
    [SerializeField] private RectTransform _slotsRoot;
    [SerializeField] private CharacterBuildSlotView _slotPrefab;
    [SerializeField] private RelicTooltipView _tooltipView;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _emptyState;

    private readonly List<CharacterBuildSlotView> _slots = new();

    public void Refresh(IReadOnlyList<RelicRuntimeState> relics)
    {
        ClearSlots();

        bool isEmpty = relics == null || relics.Count == 0;
        if (_emptyState != null)
            _emptyState.gameObject.SetActive(isEmpty);

        if (_slotsRoot == null || _slotPrefab == null || isEmpty)
        {
            ResetScrollPosition();
            return;
        }

        for (int index = 0; index < relics.Count; index++)
        {
            RelicRuntimeState state = relics[index];
            if (state?.Definition == null)
                continue;

            CharacterBuildSlotView slot = Instantiate(_slotPrefab, _slotsRoot);
            slot.name = $"PauseRelicSlot_{state.Definition.Id}";
            slot.SetContent(state.Definition.Icon,
                state.IsBroken ? "BROKEN" : $"x{state.StackCount}");

            PauseRelicSlotTooltipTrigger trigger =
                slot.GetComponent<PauseRelicSlotTooltipTrigger>() ??
                slot.gameObject.AddComponent<PauseRelicSlotTooltipTrigger>();
            trigger.Construct(state, _tooltipView);

            _slots.Add(slot);
        }

        ResetScrollPosition();
    }

    public void HideTooltip() =>
        _tooltipView?.Hide();

    public void ClearSlots()
    {
        HideTooltip();

        foreach (CharacterBuildSlotView slot in _slots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }

        _slots.Clear();
    }

    private void OnDisable() =>
        HideTooltip();

#if UNITY_EDITOR
    public void SetEditorReferences(RectTransform slotsRoot, CharacterBuildSlotView slotPrefab,
        RelicTooltipView tooltipView, ScrollRect scrollRect, RectTransform emptyState)
    {
        _slotsRoot = slotsRoot;
        _slotPrefab = slotPrefab;
        _tooltipView = tooltipView;
        _scrollRect = scrollRect;
        _emptyState = emptyState;
    }
#endif

    private void Reset()
    {
        _scrollRect = GetComponentInChildren<ScrollRect>(true);
        _slotsRoot = _scrollRect != null ? _scrollRect.content : transform as RectTransform;
        _tooltipView = GetComponentInChildren<RelicTooltipView>(true);
    }

    private void ResetScrollPosition()
    {
        if (_slotsRoot != null)
        {
            Vector2 anchoredPosition = _slotsRoot.anchoredPosition;
            anchoredPosition.y = 0f;
            _slotsRoot.anchoredPosition = anchoredPosition;
        }

        if (_scrollRect != null)
            _scrollRect.verticalNormalizedPosition = 1f;
    }
}
