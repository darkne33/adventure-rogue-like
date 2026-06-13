using UnityEngine;
using UnityEngine.Rendering;

namespace Features.Enemies.Scripts
{
    public sealed class EnemyDashView : MonoBehaviour
    {
        [SerializeField] private Material _material;
        [SerializeField] private Color _telegraphColor = new(1f, 0.08f, 0.03f, 0.85f);
        [SerializeField] private Color _dashColor = new(1f, 0.45f, 0.05f, 0.8f);

        private LineRenderer _lineRenderer;
        private TrailRenderer _trailRenderer;

        private void Awake()
        {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.material = _material;
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.positionCount = 2;
            _lineRenderer.numCapVertices = 4;
            _lineRenderer.alignment = LineAlignment.View;
            _lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _lineRenderer.receiveShadows = false;
            _lineRenderer.enabled = false;

            _trailRenderer = gameObject.AddComponent<TrailRenderer>();
            _trailRenderer.material = _material;
            _trailRenderer.time = 0.28f;
            _trailRenderer.minVertexDistance = 0.08f;
            _trailRenderer.widthMultiplier = 1.2f;
            _trailRenderer.alignment = LineAlignment.View;
            _trailRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _trailRenderer.receiveShadows = false;
            _trailRenderer.startColor = _dashColor;
            _trailRenderer.endColor = new Color(_dashColor.r, _dashColor.g, _dashColor.b, 0f);
            _trailRenderer.emitting = false;
        }

        public void ShowTelegraph(Vector3 direction, float length, float progress)
        {
            Vector3 start = transform.position + Vector3.up * 0.15f;
            float pulse = 0.5f + Mathf.Sin(progress * Mathf.PI * 6f) * 0.5f;
            float width = Mathf.Lerp(0.12f, 0.32f, pulse);
            Color color = _telegraphColor;
            color.a = Mathf.Lerp(0.35f, _telegraphColor.a, progress);

            _lineRenderer.startWidth = width;
            _lineRenderer.endWidth = width * 0.35f;
            _lineRenderer.startColor = color;
            _lineRenderer.endColor = new Color(color.r, color.g, color.b, 0.08f);
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
            _lineRenderer.enabled = false;
            _trailRenderer.emitting = false;
        }

        private void OnDisable() =>
            StopDash();
    }
}
