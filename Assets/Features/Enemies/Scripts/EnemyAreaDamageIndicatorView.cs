using DG.Tweening;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    [DisallowMultipleComponent]
    public sealed class EnemyAreaDamageIndicatorView : MonoBehaviour
    {
        private const float ScaleEpsilon = 0.0001f;

        [SerializeField] private Transform _indicator;
        [SerializeField, Min(0f)] private float _minimumRadius = 0.05f;

        private Tween _scaleTween;
        private Vector2 _targetHorizontalScale;

        public void Initialize() =>
            Hide();

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
