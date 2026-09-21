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
