using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class RoomTransitionPanel : PanelBase
{
    private const string LoadingLabel = "LOADING";
    private const int LoadingDotCount = 3;
    private const float LoadingDotInterval = 0.35f;

    [field: SerializeField] public CanvasGroup TransitionCanvasGroup { get; private set; }
    [field: SerializeField] public Image IrisImage { get; private set; }
    [field: SerializeField] public TMP_Text LoadingText { get; private set; }

    private float _loadingStartedAt;

    public void SetLoadingVisible(bool visible)
    {
        if (LoadingText == null)
            return;

        if (visible)
        {
            _loadingStartedAt = Time.unscaledTime;
            // Keep the full text width so the label stays still as the dots appear.
            LoadingText.SetText(LoadingLabel + "...");
            LoadingText.maxVisibleCharacters = LoadingLabel.Length;
        }

        LoadingText.gameObject.SetActive(visible);
    }

    private void Update()
    {
        if (LoadingText == null || !LoadingText.isActiveAndEnabled)
            return;

        int visibleDots = Mathf.FloorToInt(
            (Time.unscaledTime - _loadingStartedAt) / LoadingDotInterval) % (LoadingDotCount + 1);
        LoadingText.maxVisibleCharacters = LoadingLabel.Length + visibleDots;
    }
}
