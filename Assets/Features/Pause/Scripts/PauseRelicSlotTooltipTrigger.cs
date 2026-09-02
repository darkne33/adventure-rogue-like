using Features.Relics.Scripts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PauseRelicSlotTooltipTrigger : MonoBehaviour, IPointerEnterHandler,
    IPointerExitHandler
{
    private RelicRuntimeState _state;
    private RelicTooltipView _tooltipView;

    public void Construct(RelicRuntimeState state, RelicTooltipView tooltipView)
    {
        _state = state;
        _tooltipView = tooltipView;

        Graphic hitArea = GetComponent<Graphic>();
        if (hitArea != null)
            hitArea.raycastTarget = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_state?.Definition == null)
            return;

        _tooltipView?.Show(_state, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData) =>
        _tooltipView?.Hide();

    private void OnDisable() =>
        _tooltipView?.Hide();
}
