using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class RoomTransitionPanel : PanelBase
{
    [field: SerializeField] public CanvasGroup TransitionCanvasGroup { get; private set; }
    [field: SerializeField] public Image IrisImage { get; private set; }
    [field: SerializeField] public TMP_Text LoadingText { get; private set; }

    public void SetLoadingVisible(bool visible)
    {
        if (LoadingText != null)
            LoadingText.gameObject.SetActive(visible);
    }
}
