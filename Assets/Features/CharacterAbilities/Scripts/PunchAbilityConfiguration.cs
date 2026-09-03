using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Abilities/PunchAbilityConfiguration")]
public sealed class PunchAbilityConfiguration : AbilityConfiguration
{
    [field: Header("Combat")]
    [field: SerializeField, Min(1)] public int StartDamage { get; private set; } = 35;
    [field: SerializeField, Min(0f)] public float DamageVariationPercent { get; private set; } = 20f;
    [field: SerializeField, Min(0.05f)] public float Cooldown { get; private set; } = 2f;
    [field: SerializeField, Min(0.1f)] public float Radius { get; private set; } = 9f;

    [field: Header("Punch Sequence")]
    [field: SerializeField, Min(0f)] public float PunchInterval { get; private set; } = 0.1f;
    [field: SerializeField, Min(0.01f)] public float StartPunchSpeed { get; private set; } = 1f;
    [field: SerializeField, Min(0.01f)] public float EffectDuration { get; private set; } = 2.3f;
    [field: SerializeField] public GameObject Prefab { get; private set; }

    [field: Header("Placement and Collision")]
    [field: SerializeField, Min(0.01f)] public float ImpactRadius { get; private set; } = 1.2f;
    [field: SerializeField, Min(0f)] public float VisibleEffectOffset { get; private set; } = 0.2f;
    [field: SerializeField, Min(0f)] public float IdleEffectMinDistance { get; private set; } = 1.5f;
    [field: SerializeField, Min(0f)] public float IdleEffectMaxDistance { get; private set; } = 2.5f;

    [field: Header("Upgrades")]
    [field: SerializeField, Min(0f)] public float DamageUpgradeIncrease { get; private set; } = 2f;
    [field: SerializeField, Min(0f)] public float RadiusUpgradeIncrease { get; private set; } = 0.5f;
    [field: SerializeField, Min(0f)] public float SpeedUpgradeIncrease { get; private set; } = 0.25f;
    [field: SerializeField, Min(0f)] public float CooldownUpgradeReduction { get; private set; } = 0.05f;
}
