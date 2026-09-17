using UnityEngine;

namespace Features.Bosses.Scripts
{
    [CreateAssetMenu(menuName = "Configs/Bosses/Mushroom Head Attack",
        fileName = "MushroomHeadAttackConfiguration")]
    public sealed class MushroomHeadAttackConfiguration : MushroomAttackConfiguration
    {
        [field: Header("Head attack selection")]
        [field: SerializeField, Min(0f)] public float Range { get; private set; } = 3.2f;
        [field: SerializeField, Min(0f)] public float Cooldown { get; private set; } = 4f;

        public MushroomHeadAttackConfiguration()
        {
            ImpactNormalized = 0.4222222f;
            Radius = 2f;
            Damage = 35;
            RecoveryDuration = 0.35f;
        }
    }
}
