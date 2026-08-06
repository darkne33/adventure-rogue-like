using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Abilities/EarthRockAbilityConfiguration")]
public sealed class EarthRockAbilityConfiguration : AbilityConfiguration
{
    [field: Header("Orbit")]
    [field: SerializeField] public GameObject Prefab { get; private set; }
    [field: SerializeField] public GameObject EarthPoofPrefab { get; private set; }
    [field: SerializeField, Min(1)] public int StartStoneCount { get; private set; } = 3;
    [field: SerializeField, Min(0.1f)] public float OrbitRadius { get; private set; } = 3f;
    [field: SerializeField, Min(0f)] public float OrbitHeight { get; private set; } = 1.1f;
    [field: SerializeField, Min(0f)] public float RotationSpeed { get; private set; } = 90f;
    [field: SerializeField, Min(0f)] public float RespawnDelay { get; private set; } = 2.5f;

    [field: Header("Damage")]
    [field: SerializeField, Min(1)] public int StartDamage { get; private set; } = 10;
    [field: SerializeField, Min(0f)] public float DamageVariationPercent { get; private set; } = 20f;

    [field: Header("Upgrades")]
    [field: SerializeField, Min(0f)] public float DamageUpgradeIncrease { get; private set; } = 3f;
    [field: SerializeField, Min(0f)] public float RadiusUpgradeIncrease { get; private set; } = 0.35f;
    [field: SerializeField, Min(0f)] public float RotationSpeedUpgradeIncrease { get; private set; } = 15f;
    [field: SerializeField, Min(1)] public int StoneCountUpgradeIncrease { get; private set; } = 1;
}
