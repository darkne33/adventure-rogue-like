using UnityEngine;

namespace Features.Bosses.Scripts
{
    [CreateAssetMenu(menuName = "Configs/Bosses/Mushroom Jump Attack",
        fileName = "MushroomJumpAttackConfiguration")]
    public sealed class MushroomJumpAttackConfiguration : MushroomAttackConfiguration
    {
        [field: Header("Jump movement")]
        [field: SerializeField, Min(0f)] public float Distance { get; private set; } = 4f;
        [field: SerializeField, Range(0f, 1f)]
        [field: Tooltip("Attack_Jump clip position where forward movement begins.")]
        public float TakeoffNormalized { get; private set; } = 0.2666667f;
    }
}
