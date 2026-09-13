using UnityEngine;

namespace Features.Bosses.Scripts
{
    [CreateAssetMenu(menuName = "Configs/Bosses/Single Wood Attack",
        fileName = "WoodGuardSingleAttackConfiguration")]
    public class WoodGuardSingleAttackConfiguration : BossAttackConfiguration
    {
        [field: Header("Warning at the captured target position")]
        [field: SerializeField, Min(0.01f)] public float WarningDuration { get; private set; } = 1.2f;
        [field: SerializeField, Range(0.01f, 1f)]
        public float InitialWarningScale { get; private set; } = 0.1f;
        [field: SerializeField] public float GroundOffset { get; private set; } = 0.04f;
        [field: SerializeField]
        [field: Tooltip("Unit sphere centered on its pivot. Its size comes from the root's Hit Collider.")]
        public GameObject IndicatorPrefab { get; private set; }

        [field: Header("Roots (prefab scale is preserved)")]
        [field: SerializeField]
        [field: Tooltip("Requires WoodGuardSingleAttackPiece with its Sphere Collider assigned.")]
        public GameObject RootsPrefab { get; private set; }
        [field: SerializeField] public Vector3 RootsRotationOffset { get; private set; }
        [field: SerializeField, Min(0.01f)] public float UndergroundDepth { get; private set; } = 4f;
        [field: SerializeField, Min(0.01f)] public float RiseDuration { get; private set; } = 0.2f;
        [field: SerializeField, Min(0f)] public float HoldDuration { get; private set; } = 1.2f;
        [field: SerializeField, Min(0.01f)] public float SinkDuration { get; private set; } = 0.35f;

        [field: Header("One hit when the roots emerge")]
        [field: SerializeField, Min(0)] public int Damage { get; private set; } = 10;
        [field: SerializeField, Min(0f)] public float KnockbackForce { get; private set; } = 10f;
        [field: SerializeField, Min(0f)] public float KnockbackUpwardForce { get; private set; } = 10f;

        public float SafeWarningDuration => Mathf.Max(0.01f, WarningDuration);
        public float RootLifetime => Mathf.Max(0.01f, RiseDuration) + Mathf.Max(0f, HoldDuration) +
                                     Mathf.Max(0.01f, SinkDuration);
        public WoodGuardSingleAttackPiece PiecePrefab =>
            RootsPrefab != null ? RootsPrefab.GetComponent<WoodGuardSingleAttackPiece>() : null;

        public WoodGuardSingleAttackPiece.Geometry GetPieceGeometry() =>
            PiecePrefab.GetGeometry(Quaternion.Euler(RootsRotationOffset) * RootsPrefab.transform.localRotation);

        public Vector3 GetSphereCenter(Vector3 targetPosition, float radius) =>
            targetPosition + Vector3.up * (GroundOffset + radius);

        public override IBossAttack CreateAttack(BossFacade boss, CharacterFacade character) =>
            new WoodGuardSingleAttack(this, boss, character);

        public override void DrawPreview(Vector3 origin, Quaternion rotation)
        {
            WoodGuardSingleAttackPiece prefab = PiecePrefab;
            if (prefab == null || !prefab.HasHitCollider)
                return;
            WoodGuardSingleAttackPiece.Geometry geometry = GetPieceGeometry();
            float radius = geometry.Radius;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            Gizmos.matrix = Matrix4x4.identity;
            Vector3 center = GetSphereCenter(origin, radius);
            Vector3 indicatorPosition = center + rotation * (geometry.IndicatorOffset - geometry.CenterOffset);
            Gizmos.color = new Color(1f, 0.1f, 0.05f, 0.15f);
            Gizmos.DrawSphere(indicatorPosition, radius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(center, radius);
            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }
    }
}
