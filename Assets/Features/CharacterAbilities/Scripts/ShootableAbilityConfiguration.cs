using UnityEngine;

public abstract class ShootableAbilityConfiguration : AbilityConfiguration
{
    [field: SerializeField] public float Speed { get; set; } = 1f;
    [field: SerializeField] public int StartDamage = 2;
    [field: SerializeField, Min(0f)] public float DamageVariationPercent { get; private set; } = 20f;
    [field: SerializeField] public float Cooldown = 2f;
    [field: SerializeField] public GameObject Prefab;
    [field: SerializeField] public GameObject ExplosionPrefab;
    [field: SerializeField] public GameObject MuzzlePrefab;
}
