using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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

        [Header("Pixelated screen transition")]
        [SerializeField] private UnityEngine.UI.RawImage _pixelationImage;
        [SerializeField, Min(0.01f)] private float _pixelationDuration = 1.8f;
        [SerializeField, Range(0.01f, 1f)] private float _maximumPixelHeight = 0.5f;

        private static readonly int ProgressProperty = Shader.PropertyToID("_Progress");
        private static readonly int MaximumPixelHeightProperty = Shader.PropertyToID("_MaximumPixelHeight");

        private Material _pixelationMaterial;
        private float _loadingStartedAt;

        public void PreparePixelatedCover(RenderTexture capturedFrame)
        {
            if (_pixelationImage == null || _pixelationImage.material == null)
                throw new InvalidOperationException("The loading prefab requires a pixelation image and material.");

            if (_pixelationMaterial == null)
                _pixelationMaterial = new Material(_pixelationImage.material);

            _pixelationImage.material = _pixelationMaterial;
            _pixelationImage.texture = capturedFrame;
            _pixelationMaterial.SetFloat(ProgressProperty, 0f);
            _pixelationMaterial.SetFloat(MaximumPixelHeightProperty, _maximumPixelHeight);
            _pixelationImage.gameObject.SetActive(true);
            if (LoadingText != null)
                LoadingText.gameObject.SetActive(false);
        }

        public async UniTask PlayPixelatedCoverAsync(CancellationToken cancellationToken,
            bool showLoading = true)
        {
            using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, destroyCancellationToken);
            CancellationToken token = lifetime.Token;
            token.ThrowIfCancellationRequested();

            float startedAt = Time.unscaledTime;
            float duration = Mathf.Max(0.01f, _pixelationDuration);
            while (true)
            {
                float progress = Mathf.Clamp01((Time.unscaledTime - startedAt) / duration);
                _pixelationMaterial.SetFloat(ProgressProperty, progress);
                if (progress >= 1f)
                    break;

                await UniTask.NextFrame(cancellationToken: token);
            }

            // The prefab's black background takes over without exposing the previous screen.
            _pixelationImage.gameObject.SetActive(false);
            _pixelationImage.texture = null;
            ResetLoadingText();
            if (LoadingText != null)
                LoadingText.gameObject.SetActive(showLoading);
        }

        private void OnEnable() => ResetLoadingText();

        private void ResetLoadingText()
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
            if (LoadingText == null || !LoadingText.isActiveAndEnabled)
                return;

            int visibleDots = Mathf.FloorToInt(
                (Time.unscaledTime - _loadingStartedAt) / LoadingDotInterval) % (LoadingDotCount + 1);
            LoadingText.maxVisibleCharacters = LoadingLabel.Length + visibleDots;
        }

        private void OnDestroy()
        {
            if (_pixelationMaterial != null)
                Destroy(_pixelationMaterial);
        }
    }
}
