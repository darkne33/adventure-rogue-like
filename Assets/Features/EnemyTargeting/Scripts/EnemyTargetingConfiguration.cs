using UnityEngine;

namespace Features.EnemyTargeting
{
    [CreateAssetMenu(menuName = "Configs/Enemy Targeting", fileName = "EnemyTargetingConfiguration")]
    public sealed class EnemyTargetingConfiguration : ScriptableObject
    {
        [Header("Automatic target selection")]
        [SerializeField, Min(0.1f)] private float _targetingRange = 40f;
        [SerializeField, Min(0f)] private float _distanceWeight = 0.45f;
        [SerializeField, Min(0f)] private float _screenCenterWeight = 0.55f;
        [SerializeField, Range(0f, 1f)] private float _switchHysteresis = 0.08f;
        [Tooltip("Default, Obstacle and Wall layers. Other enemies do not obstruct selection.")]
        [SerializeField] private LayerMask _occlusionMask = 385;

        [Header("Marker presentation (Canvas units)")]
        [SerializeField, Min(0f)] private float _screenEdgePadding = 80f;
        [SerializeField, Min(0f)] private float _selectedVerticalGap;
        [SerializeField, Min(0f)] private float _bobAmplitude;
        [SerializeField, Min(0f)] private float _bobFrequency = 2f;
        [SerializeField] private Vector2 _selectedSize = new(384f, 384f);
        [SerializeField] private Vector2 _offscreenSize = new(96f, 72f);

        public float TargetingRange => Mathf.Max(0.1f, _targetingRange);
        public float DistanceWeight => Mathf.Max(0f, _distanceWeight);
        public float ScreenCenterWeight => Mathf.Max(0f, _screenCenterWeight);
        public float SwitchHysteresis => Mathf.Clamp01(_switchHysteresis);
        public LayerMask OcclusionMask => _occlusionMask;
        public float ScreenEdgePadding => Mathf.Max(0f, _screenEdgePadding);
        public float SelectedVerticalGap => Mathf.Max(0f, _selectedVerticalGap);
        public float BobAmplitude => Mathf.Max(0f, _bobAmplitude);
        public float BobFrequency => Mathf.Max(0f, _bobFrequency);
        public Vector2 SelectedSize => _selectedSize;
        public Vector2 OffscreenSize => _offscreenSize;
    }
}
