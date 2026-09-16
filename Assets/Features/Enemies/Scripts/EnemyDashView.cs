using UnityEngine;
using UnityEngine.Rendering;

namespace Features.Enemies.Scripts
{
    public sealed class EnemyDashView : MonoBehaviour
    {
        private static readonly int GradientModeId = Shader.PropertyToID("_GradientMode");
        private static readonly int StartColorId = Shader.PropertyToID("_StartColor");
        private static readonly int EndColorId = Shader.PropertyToID("_EndColor");

        [SerializeField] private Material _material;

        [Header("Telegraph")]
        [SerializeField] private Material _telegraphMaterial;
        [SerializeField] private Color _telegraphColor = new(1f, 0.08f, 0.03f, 0.85f);
        [SerializeField] private float _telegraphHeight = 0.15f;
        [SerializeField, Min(0f)] private float _telegraphMinimumWidth = 0.12f;
        [SerializeField, Min(0f)] private float _telegraphMaximumWidth = 0.32f;
        [SerializeField, Min(0f)] private float _telegraphPulseCount = 3f;
        [SerializeField, Range(0f, 1f)] private float _telegraphEndWidthMultiplier = 0.35f;
        [SerializeField, Range(0f, 1f)] private float _telegraphInitialAlpha = 0.35f;
        [SerializeField, Range(0f, 1f)] private float _telegraphEndAlpha = 0.08f;
        [SerializeField, Range(0, 16)] private int _telegraphCapVertices = 4;

        [Header("Dash Trail")]
        [SerializeField] private Color _dashColor = new(1f, 0.45f, 0.05f, 0.8f);
        [SerializeField, Min(0f)] private float _trailLifetime = 0.28f;
        [SerializeField, Min(0.001f)] private float _trailMinimumVertexDistance = 0.08f;
        [SerializeField, Min(0f)] private float _trailWidth = 1.2f;
        [SerializeField, Range(0f, 1f)] private float _trailEndAlpha;

        private LineRenderer _lineRenderer;
        private TrailRenderer _trailRenderer;
        private MaterialPropertyBlock _telegraphProperties;

        private void Awake()
        {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.sharedMaterial = _telegraphMaterial != null ? _telegraphMaterial : _material;
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.textureMode = LineTextureMode.Stretch;
            _lineRenderer.positionCount = 2;
            _lineRenderer.numCapVertices = _telegraphCapVertices;
            _lineRenderer.alignment = LineAlignment.View;
            _lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _lineRenderer.receiveShadows = false;
            _lineRenderer.enabled = false;

            if (_telegraphMaterial != null)
            {
                _telegraphProperties = new MaterialPropertyBlock();
                _telegraphProperties.SetFloat(GradientModeId, 2f);
            }

            _trailRenderer = gameObject.AddComponent<TrailRenderer>();
            _trailRenderer.material = _material;
            _trailRenderer.time = _trailLifetime;
            _trailRenderer.minVertexDistance = _trailMinimumVertexDistance;
            _trailRenderer.widthMultiplier = _trailWidth;
            _trailRenderer.alignment = LineAlignment.View;
            _trailRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _trailRenderer.receiveShadows = false;
            _trailRenderer.startColor = _dashColor;
            _trailRenderer.endColor = new Color(_dashColor.r, _dashColor.g, _dashColor.b, _trailEndAlpha);
            _trailRenderer.emitting = false;
        }

        public void ShowTelegraph(Vector3 direction, float length, float progress)
        {
            Vector3 start = transform.position + Vector3.up * _telegraphHeight;
            float pulse = 0.5f + Mathf.Sin(progress * Mathf.PI * 2f * _telegraphPulseCount) * 0.5f;
            float width = Mathf.Lerp(_telegraphMinimumWidth, _telegraphMaximumWidth, pulse);
            Color color = _telegraphColor;
            color.a = Mathf.Lerp(_telegraphInitialAlpha, _telegraphColor.a, progress);

            _lineRenderer.startWidth = width;
            _lineRenderer.endWidth = width * _telegraphEndWidthMultiplier;
            _lineRenderer.startColor = color;
            _lineRenderer.endColor = new Color(color.r, color.g, color.b, _telegraphEndAlpha);
            if (_telegraphProperties != null)
            {
                Color tint = new(color.r, color.g, color.b, 1f);
                Color startColor = _telegraphMaterial.GetColor(StartColorId) * tint;
                Color endColor = _telegraphMaterial.GetColor(EndColorId) * tint;
                startColor.a = color.a;
                endColor.a = _telegraphEndAlpha;
                _telegraphProperties.SetColor(StartColorId, startColor);
                _telegraphProperties.SetColor(EndColorId, endColor);
                _lineRenderer.SetPropertyBlock(_telegraphProperties);
            }
            _lineRenderer.SetPosition(0, start);
            _lineRenderer.SetPosition(1, start + direction * length);
            _lineRenderer.enabled = true;
        }

        public void StartDash()
        {
            _lineRenderer.enabled = false;
            _trailRenderer.Clear();
            _trailRenderer.emitting = true;
        }

        public void StopDash()
        {
            if (_lineRenderer != null)
                _lineRenderer.enabled = false;
            if (_trailRenderer != null)
                _trailRenderer.emitting = false;
        }

        private void OnDisable() =>
            StopDash();
    }
}
