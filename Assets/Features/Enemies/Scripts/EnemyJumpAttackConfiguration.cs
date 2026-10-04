using UnityEngine;

namespace Features.Enemies.Scripts
{
    [CreateAssetMenu(menuName = "Configs/Enemies/Jump Attack", fileName = "EnemyJumpAttackConfiguration")]
    public sealed class EnemyJumpAttackConfiguration : ScriptableObject
    {
        [field: Header("Jump timing and destination")]
        [field: SerializeField, Min(0.1f)] public float Interval { get; private set; } = 5f;
        [field: SerializeField, Min(0.01f)] public float Duration { get; private set; } = 2.5f;
        [field: SerializeField, Range(0f, 0.99f)] public float TakeoffNormalized { get; private set; } = 0.2666667f;
        [field: SerializeField, Range(0.01f, 1f)] public float ImpactNormalized { get; private set; } = 0.6666667f;
        [field: SerializeField, Min(0.1f)] public float Height { get; private set; } = 4f;
        [field: SerializeField, Min(0f)] public float TargetOffsetRadius { get; private set; } = 3.5f;
        [field: SerializeField, Range(0f, 1f)] public float DirectTargetChance { get; private set; } = 0.3333333f;

        [field: Header("Landing damage and warning")]
        [field: SerializeField, Min(0.01f)] public float Radius { get; private set; } = 7f;
        [field: SerializeField] public GameObject IndicatorPrefab { get; private set; }
        [field: SerializeField, Range(0.01f, 1f)] public float InitialIndicatorScale { get; private set; } = 0.05f;
        [field: SerializeField] public float IndicatorGroundOffset { get; private set; } = 0.05f;
        [field: SerializeField, Min(0f)] public float KnockbackForce { get; private set; } = 7f;
        [field: SerializeField, Min(0f)] public float KnockbackUpwardForce { get; private set; } = 7f;

        [field: Header("Landing effect")]
        [field: SerializeField] public GameObject EffectPrefab { get; private set; }
        [field: SerializeField, Min(0.01f)] public float EffectScale { get; private set; } = 0.7f;
        [field: SerializeField, Min(0.1f)] public float EffectLifetime { get; private set; } = 3f;

        [field: Header("Floor and obstacles")]
        [field: SerializeField] public LayerMask GroundMask { get; private set; } = 1 << 6;
        [field: SerializeField] public LayerMask ObstacleMask { get; private set; } = (1 << 7) | (1 << 8) | (1 << 11);
        [field: SerializeField, Min(0.01f)] public float GroundProbeHeight { get; private set; } = 4f;
        [field: SerializeField, Min(0.01f)] public float GroundProbeDistance { get; private set; } = 10f;
    }
}
