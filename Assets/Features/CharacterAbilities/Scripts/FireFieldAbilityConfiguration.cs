using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Abilities/FireFieldAbilityConfiguration")]
public sealed class FireFieldAbilityConfiguration : AbilityConfiguration
{
    [field: Header("Field")]
    [field: SerializeField] public GameObject Prefab { get; private set; }
    [field: SerializeField, Min(0.1f)] public float DistancePerField { get; private set; } = 3f;
    [field: SerializeField, Min(0.1f)] public float MinimumDistancePerField { get; private set; } = 1.5f;
    [field: SerializeField, Min(0.1f)] public float FieldDuration { get; private set; } = 3f;
    [field: SerializeField, Min(0.05f)] public float DamageTickInterval { get; private set; } = 0.5f;
    [field: SerializeField, Min(0.1f)] public float DamageRadius { get; private set; } = 2f;
    [field: SerializeField, Min(0.1f)] public float DamageHeight { get; private set; } = 2.5f;

    [field: Header("Damage")]
    [field: SerializeField, Min(1)] public int StartDamage { get; private set; } = 4;
    [field: SerializeField, Min(0f)] public float DamageVariationPercent { get; private set; } = 20f;
    [field: SerializeField, Min(0f)] public float DamageUpgradeIncrease { get; private set; } = 2f;

    [field: Header("Upgrades")]
    [field: SerializeField, Min(0f)] public float DistanceUpgradeReduction { get; private set; } = 0.25f;
    [field: SerializeField, Min(0f)] public float RadiusUpgradeIncrease { get; private set; } = 0.35f;

    [field: Header("Ground Snap")]
    [field: SerializeField, Min(0f)] public float GroundRayStartHeight { get; private set; } = 4f;
    [field: SerializeField, Min(0.1f)] public float GroundRayDistance { get; private set; } = 12f;
    [field: SerializeField, Min(0f)] public float GroundOffset { get; private set; } = 0.05f;
}
