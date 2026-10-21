using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Abilities/LightningStaffAbilityConfiguration")]
public sealed class LightningStaffAbilityConfiguration : AbilityConfiguration
{
    [field: Header("Lightning Staff")]
    [field: SerializeField] public LightningBoltView Prefab { get; private set; }
    [field: SerializeField, Min(1f)] public float StartDamage { get; private set; } = 7f;
    [field: SerializeField, Min(1)] public int StartProjectileCount { get; private set; } = 1;
    [field: SerializeField, Min(0)] public int StartBounces { get; private set; }
    [field: SerializeField, Min(0f)] public float Knockback { get; private set; } = 1f;
    [field: SerializeField, Min(0.1f)] public float TargetRange { get; private set; } = 40f;
    [field: SerializeField, Min(0.01f)] public float AttackDuration { get; private set; } = 1f;
    [field: SerializeField, Min(0.01f)] public float EndCooldown { get; private set; } = 1.1f;
    [field: SerializeField, Min(0.01f)] public float BurstInterval { get; private set; } = 0.3f;
    [field: SerializeField, Min(0.01f)] public float MinimumBurstInterval { get; private set; } = 0.04f;

    [field: Header("Strike and chain geometry")]
    [field: SerializeField, Min(0.1f)] public float StrikeRadius { get; private set; } = 1.5f;
    [field: SerializeField, Min(0.1f)] public float StrikeHeight { get; private set; } = 2.5f;
    [field: SerializeField, Min(0.1f)] public float BounceRange { get; private set; } = 10f;
    [field: SerializeField, Min(0.01f)] public float BounceInterval { get; private set; } = 0.075f;
    [field: SerializeField, Range(0f, 1f)] public float BounceDamageMultiplier { get; private set; } = 0.5f;
    [field: SerializeField, Min(0.1f)] public float SkyHeight { get; private set; } = 12f;
    [field: SerializeField, Min(0.01f)] public float BoltLifetime { get; private set; } = 0.18f;
    [field: SerializeField, Min(0f)] public float VisionHeightOffset { get; private set; } = 1f;

    [field: Header("Common upgrades")]
    [field: SerializeField, Min(0f)] public float DamageUpgradeIncrease { get; private set; } = 2f;
    [field: SerializeField, Min(0f)] public float ProjectileCountUpgradeIncrease { get; private set; } = 1f;
    [field: SerializeField, Min(0f)] public float BounceUpgradeIncrease { get; private set; } = 1f;
    [field: SerializeField, Min(0f)] public float SizeUpgradeIncrease { get; private set; } = 0.2f;
}
