using UnityEngine;

namespace Features.Bosses.Scripts
{
    [CreateAssetMenu(menuName = "Configs/Bosses/Ogre Boss", fileName = "OgreBossConfiguration")]
    public sealed class OgreBossConfiguration : ScriptableObject
    {
        [field: SerializeField, Min(0f)] public float MoveSpeed { get; private set; } = 4.8f;
        [field: SerializeField, Min(0.01f)] public float BodyRadius { get; private set; } = 1f;
        [field: SerializeField] public LayerMask MovementObstacleMask { get; private set; }
        [field: SerializeField] public LayerMask GroundMask { get; private set; }
        [field: SerializeField, Min(0.01f)] public float GroundProbeHeight { get; private set; } = 4f;
        [field: SerializeField, Min(0.01f)] public float GroundProbeDistance { get; private set; } = 10f;
        [field: SerializeField, Min(1f)] public float DamageReactionCooldown { get; private set; } = 1f;
    }
}
