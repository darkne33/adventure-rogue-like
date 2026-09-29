using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Abilities/AuraAbilityConfiguration")]
public sealed class AuraAbilityConfiguration : AbilityConfiguration
{
    [field: Header("Aura")]
    [field: SerializeField] public AuraDamageArea Prefab { get; private set; }
    [field: SerializeField, Min(0.1f)] public float DamageRadius { get; private set; } = 7f;
    [field: SerializeField, Min(0.1f)] public float DamageHeight { get; private set; } = 2.5f;
    [field: SerializeField, Min(0.05f)] public float DamageTickInterval { get; private set; } = 0.5f;
    [field: SerializeField, Min(0.05f)] public float MinimumTickInterval { get; private set; } = 0.15f;
    [field: SerializeField, Min(0.01f)] public float VisualBaseRadius { get; private set; } = 1f;
    [field: SerializeField] public float VisualHeightOffset { get; private set; } = 0.15f;

    [field: Header("Damage")]
    [field: SerializeField, Min(1)] public int StartDamage { get; private set; } = 4;
    [field: SerializeField, Range(0f, 100f)] public float DamageVariationPercent { get; private set; } = 20f;

    [field: Header("Upgrades")]
    [field: SerializeField, Min(0f)] public float DamageUpgradeIncrease { get; private set; } = 2f;
    [field: SerializeField, Min(0f)] public float TickIntervalUpgradeReduction { get; private set; } = 0.05f;
    [field: SerializeField, Min(0f)] public float RadiusUpgradeIncrease { get; private set; } = 0.35f;
}
