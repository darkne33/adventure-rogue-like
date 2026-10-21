using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Abilities/RabbitBoomerangAbilityConfiguration")]
public class RabbitBoomerangAbilityConfiguration : ShootableAbilityConfiguration
{
    [field: SerializeField, Min(1)] public int StartProjectileCount { get; private set; } = 1;
    [field: Header("Upgrades")]
    [field: SerializeField, Min(0f)] public float DamageUpgradeIncrease { get; private set; } = 15f;
    [field: SerializeField, Min(0f)] public float SpeedUpgradeIncrease { get; private set; } = 5f;
    [field: SerializeField, Min(0f)] public float SizeUpgradeIncrease { get; private set; } = 0.15f;
    [field: Header("Flight")]
    [field: SerializeField, Min(0.1f)] public float TargetingRange { get; private set; } = 40f;
    [field: SerializeField, Min(0.01f)] public float OutboundDuration { get; private set; } = 1f;
    [field: SerializeField, Min(0.01f)] public float ReturnSpeedMultiplier { get; private set; } = 1.5f;
    [field: SerializeField, Min(0.01f)] public float ReturnAcceleration { get; private set; } = 60f;
    [field: SerializeField, Min(0.01f)] public float CollectRadius { get; private set; } = 0.8f;
    [field: SerializeField, Min(0.01f)] public float HitCooldown { get; private set; } = 0.3f;
    [field: SerializeField, Min(0.01f)] public float MaxLifetime { get; private set; } = 10f;
    [field: SerializeField, Min(0f)] public float CollisionSkin { get; private set; } = 0.025f;
}
