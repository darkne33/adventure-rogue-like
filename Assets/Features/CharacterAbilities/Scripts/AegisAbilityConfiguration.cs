using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Abilities/AegisAbilityConfiguration")]
public sealed class AegisAbilityConfiguration : AbilityConfiguration
{
    [field: Header("Shields")]
    [field: SerializeField] public AegisShieldView Prefab { get; private set; }
    [field: SerializeField, Min(1)] public int StartShieldCount { get; private set; } = 2;
    [field: SerializeField, Min(0f)] public float StartRotationSpeed { get; private set; } = 55f;

    [field: Header("Shockwave")]
    [field: SerializeField, Min(1)] public int StartDamage { get; private set; } = 10;
    [field: SerializeField, Min(0.1f)] public float ShockwaveRadius { get; private set; } = 6f;
    [field: SerializeField, Range(1f, 360f)] public float ShockwaveAngle { get; private set; } = 120f;
    [field: SerializeField, Min(0.1f)] public float ShockwaveHeight { get; private set; } = 2.5f;
    [field: SerializeField, Min(0.05f)] public float ShockwaveDuration { get; private set; } = 1.5f;

    [field: Header("Upgrades")]
    [field: SerializeField, Min(0f)] public float DamageUpgradeIncrease { get; private set; } = 2f;
    [field: SerializeField, Min(0f)] public float RotationSpeedUpgradeIncrease { get; private set; } = 15f;
    [field: SerializeField, Min(0f)] public float SizeUpgradeIncrease { get; private set; } = 0.15f;
}
