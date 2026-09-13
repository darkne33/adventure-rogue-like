using TMPro;
using UnityEngine;

namespace UI
{
    public sealed class LoadingPanel : PanelBase
    {
        private const string LoadingLabel = "LOADING";
        private const int LoadingDotCount = 3;
        private const float LoadingDotInterval = 0.35f;

        [field: SerializeField] public TMP_Text LoadingText { get; private set; }

        private float _loadingStartedAt;

        private void OnEnable()
        {
            _loadingStartedAt = Time.unscaledTime;
            if (LoadingText == null)
                return;

            // Reveal dots without changing the text width or moving the label.
            LoadingText.SetText(LoadingLabel + "...");
            LoadingText.maxVisibleCharacters = LoadingLabel.Length;
        }

        private void Update()
        {
            if (LoadingText == null)
                return;

            int visibleDots = Mathf.FloorToInt(
                (Time.unscaledTime - _loadingStartedAt) / LoadingDotInterval) % (LoadingDotCount + 1);
            LoadingText.maxVisibleCharacters = LoadingLabel.Length + visibleDots;
        }
    }
}
