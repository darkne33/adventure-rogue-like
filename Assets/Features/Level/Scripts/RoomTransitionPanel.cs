using UI;
using UnityEngine;
using UnityEngine.UI;

public class RoomTransitionPanel : PanelBase
{
    [field: SerializeField] public CanvasGroup TransitionCanvasGroup { get; private set; }
    [field: SerializeField] public Image IrisImage { get; private set; }
}
