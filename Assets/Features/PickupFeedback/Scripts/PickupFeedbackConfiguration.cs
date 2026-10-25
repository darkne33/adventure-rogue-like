using System;
using UnityEngine;

namespace Features.PickupFeedback
{
    public enum PickupFeedbackKind
    {
        Item,
        Silver,
        Key,
        Relic
    }

    [Serializable]
    public sealed class PickupFeedbackStyle
    {
        [SerializeField, Min(0.1f)] private float _duration;
        [SerializeField, Min(1f)] private float _iconSize;
        [SerializeField] private Color _color;
        [SerializeField, Range(0, 12)] private int _sparkCount;
        [SerializeField, Min(0f)] private float _spreadRadius;

        public float Duration => _duration;
        public float IconSize => _iconSize;
        public Color Color => _color;
        public int SparkCount => _sparkCount;
        public float SpreadRadius => _spreadRadius;

        public PickupFeedbackStyle(float duration, float iconSize, Color color, int sparkCount,
            float spreadRadius)
        {
            _duration = duration;
            _iconSize = iconSize;
            _color = color;
            _sparkCount = sparkCount;
            _spreadRadius = spreadRadius;
        }
    }

    [CreateAssetMenu(menuName = "Configs/Character/Pickup Feedback")]
    public sealed class PickupFeedbackConfiguration : ScriptableObject
    {
        public const string Address =
            "Assets/Features/PickupFeedback/Configs/PickupFeedbackConfiguration.asset";

        [SerializeField] private CharacterPickupFeedbackView _viewPrefab;
        [SerializeField] private Sprite _silverIcon;
        [SerializeField] private Sprite _keyIcon;
        [SerializeField, Min(0.001f)] private float _worldScale = 0.01f;
        [SerializeField, Min(0f)] private float _headClearance = 0.55f;
        [SerializeField, Min(0.01f)] private float _popDuration = 0.28f;
        [SerializeField, Min(0.01f)] private float _ringDuration = 0.43f;
        [SerializeField, Min(0.01f)] private float _flashDuration = 0.2f;
        [SerializeField, Min(0.01f)] private float _fadeDuration = 0.27f;
        [SerializeField, Min(0f)] private float _riseDistance = 18f;
        [SerializeField] private PickupFeedbackStyle _item = new(1.15f, 72f,
            new Color(1f, 0.855f, 0.561f, 1f), 12, 60f);
        [SerializeField] private PickupFeedbackStyle _silver = new(0.72f, 54f,
            new Color(0.871f, 0.949f, 1f, 1f), 7, 38f);
        [SerializeField] private PickupFeedbackStyle _key = new(1.05f, 70f,
            new Color(1f, 0.863f, 0.533f, 1f), 12, 60f);
        [SerializeField] private PickupFeedbackStyle _relic = new(1.4f, 76f,
            new Color(0.914f, 0.725f, 1f, 1f), 12, 60f);

        public CharacterPickupFeedbackView ViewPrefab => _viewPrefab;
        public Sprite SilverIcon => _silverIcon;
        public Sprite KeyIcon => _keyIcon;
        public float WorldScale => _worldScale;
        public float HeadClearance => _headClearance;
        public float PopDuration => _popDuration;
        public float RingDuration => _ringDuration;
        public float FlashDuration => _flashDuration;
        public float FadeDuration => _fadeDuration;
        public float RiseDistance => _riseDistance;

        public PickupFeedbackStyle GetStyle(PickupFeedbackKind kind) => kind switch
        {
            PickupFeedbackKind.Silver => _silver,
            PickupFeedbackKind.Key => _key,
            PickupFeedbackKind.Relic => _relic,
            _ => _item
        };
    }
}
