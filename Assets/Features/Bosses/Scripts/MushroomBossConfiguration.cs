using UnityEngine;

namespace Features.Bosses.Scripts
{
    [CreateAssetMenu(menuName = "Configs/Bosses/Mushroom Boss", fileName = "MushroomBossConfiguration")]
    public sealed class MushroomBossConfiguration : ScriptableObject
    {
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
