using UnityEngine;

namespace Features.Bosses.Scripts
{
    public enum WoodGuardPieceSequence
    {
        Simultaneous,
        LeftToRight,
        RightToLeft,
        CenterOut
    }

    [CreateAssetMenu(menuName = "Configs/Bosses/Horizontal Wood Attack",
        fileName = "WoodGuardHorizontalAttackConfiguration")]
    public sealed class WoodGuardHorizontalAttackConfiguration : BossAttackConfiguration
    {
        [field: Header("Lines made of pieces (prefab scale is preserved)")]
        [field: SerializeField, Min(1)] public int LineCount { get; private set; } = 3;
        [field: SerializeField, Min(1)] public int PiecesPerLine { get; private set; } = 10;
        [field: SerializeField, Min(0f)]
        [field: Tooltip("Clear space between neighbouring piece colliders. Zero places them edge to edge.")]
        public float PieceGap { get; private set; }
        [field: SerializeField, Min(0f)]
        [field: Tooltip("Clear space between the edges of neighbouring lines.")]
        public float Gap { get; private set; } = 3f;
        [field: SerializeField, Min(0f)]
        [field: Tooltip("Distance from Attack Origin to the nearest edge of the first line.")]
        public float FirstLineOffset { get; private set; } = 4f;

        [field: Header("Warning and wave")]
        [field: SerializeField, Min(0.01f)] public float WarningDuration { get; private set; } = 1.2f;
        [field: SerializeField, Min(0.01f)]
        [field: Tooltip("Delay between the STARTS of consecutive warnings; warnings can overlap.")]
        public float LineStartInterval { get; private set; } = 0.45f;
        [field: SerializeField] public WoodGuardPieceSequence PieceSequence { get; private set; }
        [field: SerializeField, Min(0f)]
        [field: Tooltip("Delay between piece warnings within a line, in the selected sequence.")]
        public float PieceStartInterval { get; private set; } = 0.1f;
        [field: SerializeField, Range(0.01f, 1f)]
        [field: Tooltip("Starting fraction of the warning's full size on all three axes.")]
        public float InitialWarningScale { get; private set; } = 0.1f;
        [field: SerializeField] public float GroundOffset { get; private set; } = 0.04f;
        [field: SerializeField] public Material IndicatorMaterial { get; private set; }

        [field: Header("Roots (prefab scale is preserved)")]
        [field: SerializeField]
        [field: Tooltip("Requires WoodGuardHorizontalAttackPiece with its Hit Collider assigned.")]
        public GameObject RootsPrefab { get; private set; }
        [field: SerializeField] public Vector3 RootsLocalOffset { get; private set; }
        [field: SerializeField] public Vector3 RootsRotationOffset { get; private set; }
        [field: SerializeField, Min(0.01f)] public float UndergroundDepth { get; private set; } = 4f;
        [field: SerializeField, Min(0.01f)] public float RiseDuration { get; private set; } = 0.2f;
        [field: SerializeField, Min(0f)] public float HoldDuration { get; private set; } = 1.2f;
        [field: SerializeField, Min(0.01f)] public float SinkDuration { get; private set; } = 0.35f;

        [field: Header("At most one hit per line, when a piece emerges")]
        [field: SerializeField, Min(0)] public int Damage { get; private set; } = 10;
        [field: SerializeField, Min(0f)]
        [field: Tooltip("Horizontal impulse away from the line, like BunEnemy's dash knockback.")]
        public float KnockbackForce { get; private set; } = 10f;
        [field: SerializeField, Min(0f)] public float KnockbackUpwardForce { get; private set; } = 10f;

        public int Count => Mathf.Max(1, LineCount);
        public int PieceCount => Mathf.Max(1, PiecesPerLine);
        public float SafeWarningDuration => Mathf.Max(0.01f, WarningDuration);
        public float SafeLineStartInterval => Mathf.Max(0.01f, LineStartInterval);
        public float PieceWaveDuration => Mathf.Max(GetPieceStartDelay(0), GetPieceStartDelay(PieceCount - 1));
        public float TelegraphDuration => SafeWarningDuration + (Count - 1) * SafeLineStartInterval +
                                          PieceWaveDuration;
        public float RootLifetime => Mathf.Max(0.01f, RiseDuration) + Mathf.Max(0f, HoldDuration) +
                                     Mathf.Max(0.01f, SinkDuration);
        public Vector3 GetLineCenter(Vector3 origin, Quaternion rotation, int index, Vector3 pieceFootprint) =>
            origin + rotation * Vector3.forward * (Mathf.Max(0f, FirstLineOffset) +
                pieceFootprint.z * 0.5f + index * (pieceFootprint.z + Mathf.Max(0f, Gap))) +
            rotation * RootsLocalOffset;

        public Vector3 GetPieceCenter(Vector3 lineCenter, Quaternion rotation, int index,
            Vector3 pieceFootprint) => lineCenter + rotation * Vector3.right *
                ((index - (PieceCount - 1) * 0.5f) * (pieceFootprint.x + Mathf.Max(0f, PieceGap))) +
            Vector3.up * (GroundOffset + pieceFootprint.y * 0.5f);

        public float GetPieceStartDelay(int index)
        {
            int order = PieceSequence switch
            {
                WoodGuardPieceSequence.LeftToRight => index,
                WoodGuardPieceSequence.RightToLeft => PieceCount - 1 - index,
                WoodGuardPieceSequence.CenterOut => Mathf.Abs(2 * index - (PieceCount - 1)) / 2,
                _ => 0
            };
            return order * Mathf.Max(0f, PieceStartInterval);
        }

        public WoodGuardHorizontalAttackPiece PiecePrefab =>
            RootsPrefab != null ? RootsPrefab.GetComponent<WoodGuardHorizontalAttackPiece>() : null;

        public WoodGuardHorizontalAttackPiece.Geometry GetPieceGeometry() =>
            PiecePrefab.GetGeometry(Quaternion.Euler(RootsRotationOffset) * RootsPrefab.transform.localRotation);

        public override IBossAttack CreateAttack(BossFacade boss, CharacterFacade character) =>
            new WoodGuardHorizontalAttack(this, boss, character);

        public override void DrawPreview(Vector3 origin, Quaternion rotation)
        {
            WoodGuardHorizontalAttackPiece prefab = PiecePrefab;
            if (prefab == null || !prefab.HasHitCollider)
                return;
            WoodGuardHorizontalAttackPiece.Geometry geometry = GetPieceGeometry();
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            for (int i = 0; i < Count; i++)
            {
                Vector3 lineCenter = GetLineCenter(origin, rotation, i, geometry.Footprint);
                for (int j = 0; j < PieceCount; j++)
                {
                    Vector3 center = GetPieceCenter(lineCenter, rotation, j, geometry.Footprint);
                    Gizmos.matrix = Matrix4x4.TRS(center, rotation * geometry.BoxRotation, Vector3.one);
                    Gizmos.color = new Color(1f, 0.1f, 0.05f, 0.15f);
                    Gizmos.DrawCube(Vector3.zero, geometry.Size);
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireCube(Vector3.zero, geometry.Size);
#if UNITY_EDITOR
                    UnityEditor.Handles.Label(center,
                        $"Line {i + 1}, piece {j + 1}: {i * SafeLineStartInterval + GetPieceStartDelay(j):0.##}s");
#endif
                }
            }
            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }
    }
}
