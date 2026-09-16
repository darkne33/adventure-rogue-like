using DG.Tweening;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    [DisallowMultipleComponent]
    public sealed class EnemyAreaDamageIndicatorView : MonoBehaviour
    {
        private const float ScaleEpsilon = 0.0001f;
        private static readonly int GradientModeId = Shader.PropertyToID("_GradientMode");
        private static readonly int GradientAxisId = Shader.PropertyToID("_GradientAxis");
        private static readonly int GradientOriginId = Shader.PropertyToID("_GradientOrigin");
        private static readonly int GradientLengthId = Shader.PropertyToID("_GradientLength");

        [SerializeField] private Transform _indicator;
        [SerializeField, Min(0f)] private float _minimumRadius = 0.05f;

        private Tween _scaleTween;
        private Vector2 _targetHorizontalScale;

        public void Initialize()
        {
            ConfigureHeightGradient();
            Hide();
        }

        public void Show(Vector3 worldCenter, float worldRadius, float duration)
        {
            if (_indicator == null)
            {
                Debug.LogError($"{name} has no area damage indicator assigned.", this);
                return;
            }

            KillScaleTween();
            SetHorizontalPosition(worldCenter);

            float safeRadius = Mathf.Max(0f, worldRadius);
            Vector2 startScale = GetHorizontalScale(Mathf.Min(_minimumRadius, safeRadius));
            _targetHorizontalScale = GetHorizontalScale(safeRadius);

            SetHorizontalScale(startScale);
            _indicator.gameObject.SetActive(true);

            float safeDuration = Mathf.Max(0f, duration);
            if (safeDuration <= 0f)
            {
                SetHorizontalScale(_targetHorizontalScale);
                return;
            }

            _scaleTween = DOTween.To(
                    () => 0f,
                    progress => SetHorizontalScale(
                        Vector2.LerpUnclamped(startScale, _targetHorizontalScale, progress)),
                    1f,
                    safeDuration)
                .SetEase(Ease.Linear)
                .SetLink(gameObject);
        }

        public void Complete(Vector3 worldCenter)
        {
            if (_indicator == null)
                return;

            KillScaleTween();
            SetHorizontalPosition(worldCenter);
            SetHorizontalScale(_targetHorizontalScale);
        }

        public void Hide()
        {
            KillScaleTween();

            if (_indicator == null)
                return;

            SetHorizontalScale(GetHorizontalScale(_minimumRadius));
            _indicator.gameObject.SetActive(false);
        }

        private void ConfigureHeightGradient()
        {
            if (_indicator == null)
                return;

            MeshFilter meshFilter = _indicator.GetComponent<MeshFilter>();
            Renderer indicatorRenderer = _indicator.GetComponent<Renderer>();
            if (meshFilter == null || meshFilter.sharedMesh == null || indicatorRenderer == null)
                return;

            Bounds bounds = meshFilter.sharedMesh.bounds;
            Vector3 gradientOrigin = bounds.center;
            gradientOrigin.y = bounds.min.y;

            var properties = new MaterialPropertyBlock();
            indicatorRenderer.GetPropertyBlock(properties);
            properties.SetFloat(GradientModeId, 1f);
            properties.SetVector(GradientAxisId, Vector3.up);
            properties.SetVector(GradientOriginId, gradientOrigin);
            properties.SetFloat(GradientLengthId, Mathf.Max(ScaleEpsilon, bounds.size.y));
            indicatorRenderer.SetPropertyBlock(properties);
        }

        private Vector2 GetHorizontalScale(float worldRadius)
        {
            float meshSizeX = 1f;
            float meshSizeZ = 1f;
            MeshFilter meshFilter = _indicator.GetComponent<MeshFilter>();

            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Vector3 meshSize = meshFilter.sharedMesh.bounds.size;
                meshSizeX = Mathf.Max(ScaleEpsilon, Mathf.Abs(meshSize.x));
                meshSizeZ = Mathf.Max(ScaleEpsilon, Mathf.Abs(meshSize.z));
            }

            Vector3 parentScale = _indicator.parent != null
                ? _indicator.parent.lossyScale
                : Vector3.one;
            float worldDiameter = Mathf.Max(0f, worldRadius) * 2f;

            return new Vector2(
                worldDiameter / Mathf.Max(
                    ScaleEpsilon, meshSizeX * Mathf.Abs(parentScale.x)),
                worldDiameter / Mathf.Max(
                    ScaleEpsilon, meshSizeZ * Mathf.Abs(parentScale.z)));
        }

        private void SetHorizontalPosition(Vector3 worldCenter)
        {
            Vector3 position = _indicator.position;
            position.x = worldCenter.x;
            position.z = worldCenter.z;
            _indicator.position = position;
        }

        private void SetHorizontalScale(Vector2 horizontalScale)
        {
            Vector3 scale = _indicator.localScale;
            scale.x = horizontalScale.x;
            scale.z = horizontalScale.y;
            _indicator.localScale = scale;
        }

        private void KillScaleTween()
        {
            _scaleTween?.Kill();
            _scaleTween = null;
        }

        private void OnDisable() =>
            Hide();

        private void OnDestroy() =>
            KillScaleTween();
    }
}
