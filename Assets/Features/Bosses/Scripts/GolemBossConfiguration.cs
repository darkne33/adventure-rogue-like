using UnityEngine;

namespace Features.Bosses.Scripts
{
    [CreateAssetMenu(menuName = "Configs/Bosses/Golem Boss", fileName = "GolemBossConfiguration")]
    public sealed class GolemBossConfiguration : ScriptableObject
    {
        [field: SerializeField, Min(0f)] public float MoveSpeed { get; private set; } = 4.8f;
        [field: SerializeField, Min(0.01f)] public float BodyRadius { get; private set; } = 1f;
        [field: SerializeField] public LayerMask MovementObstacleMask { get; private set; } =
            (1 << 7) | (1 << 8) | (1 << 11);
        [field: SerializeField] public LayerMask GroundMask { get; private set; } = 1 << 6;
        [field: SerializeField, Min(0.01f)] public float GroundProbeHeight { get; private set; } = 4f;
        [field: SerializeField, Min(0.01f)] public float GroundProbeDistance { get; private set; } = 10f;
    }
}
