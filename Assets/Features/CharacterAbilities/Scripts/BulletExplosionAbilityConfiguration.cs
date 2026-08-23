using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Abilities/BulletExplosionAbilityConfiguration")]
public sealed class BulletExplosionAbilityConfiguration : ShootableAbilityConfiguration
{
    [field: SerializeField, Min(1)] public int StartProjectileCount { get; private set; } = 4;

    [field: Header("Upgrades")]
    [field: SerializeField, Min(0f)] public float DamageUpgradeIncrease { get; private set; } = 1f;
    [field: SerializeField, Min(0f)] public float SpeedUpgradeIncrease { get; private set; } = 7f;
    [field: SerializeField, Min(0f)] public float CooldownUpgradeReduction { get; private set; } = 0.2f;
    [field: SerializeField] public float TravelDistance { get; private set; } = 100f;
}
