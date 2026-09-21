using UnityEngine;

namespace Features.Bosses.Scripts
{
    [CreateAssetMenu(menuName = "Configs/Bosses/Mushroom Boss", fileName = "MushroomBossConfiguration")]
    public sealed class MushroomBossConfiguration : ScriptableObject
    {
        [field: Header("Splitting")]
        [field: SerializeField]
        [field: Tooltip("The boss splits once into two smaller bosses. The children cannot split again.")]
        public MushroomBossFacade SplitPrefab { get; private set; }
        [field: SerializeField, Range(0f, 100f)]
        public float SplitHealthPercentage { get; private set; } = 50f;
        [field: SerializeField, Range(0.01f, 1f)]
        public float SplitScaleMultiplier { get; private set; } = 0.5f;

        [field: Header("Split appearance")]
        [field: SerializeField, Min(0.01f)]
        public float SplitInflationDuration { get; private set; } = 1.7f;
        [field: SerializeField]
        public Vector3 SplitInflationScale { get; private set; } = new Vector3(1.5f, 1.18f, 1.3f);
        [field: SerializeField, Min(0.01f)]
        public float SplitJumpDuration { get; private set; } = 0.7f;
        [field: SerializeField, Min(0f)]
        public float SplitJumpHeight { get; private set; } = 1.6f;
        [field: SerializeField, Min(0f)]
        [field: Tooltip("Outward jump distance, shortened by the existing wall and floor probes.")]
        public float SplitJumpDistance { get; private set; } = 2.5f;

        [field: Header("Split children movement")]
        [field: SerializeField, Range(10f, 120f)]
        [field: Tooltip("Only split children: flank the character from opposite sides at this angle in degrees.")]
        public float SplitFlankAngle { get; private set; } = 65f;
        [field: SerializeField, Min(0f)]
        [field: Tooltip("Only split children: extra clearance between their bodies and jump paths.")]
        public float SplitSeparationPadding { get; private set; } = 0.75f;
        [field: SerializeField]
        [field: Tooltip("Only split children: random additional delay before the first attack and after each attack, in boss-time seconds (minimum, maximum).")]
        public Vector2 SplitAttackDelayRange { get; private set; } = new Vector2(0.15f, 0.85f);

        [field: Header("Ground and obstacles")]
        [field: SerializeField]
        [field: Tooltip("Obstacle, Wall and Door layers in this project. Excludes the character and enemies.")]
        public LayerMask MovementObstacleMask { get; private set; } = (1 << 7) | (1 << 8) | (1 << 11);
        [field: SerializeField] public LayerMask GroundMask { get; private set; } = 1 << 6;
        [field: SerializeField, Min(0f)] public float GroundProbeHeight { get; private set; } = 4f;
        [field: SerializeField, Min(0.01f)] public float GroundProbeDistance { get; private set; } = 10f;
        [field: SerializeField, Min(0.01f)] public float BodyRadius { get; private set; } = 1f;
    }
}
