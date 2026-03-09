using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Abilities/ShootableAbilityConfiguration")]
public class ShootableAbilityConfiguration : AbilityConfiguration
{
    [field: SerializeField] public float Speed { get; set; } = 1f;
    [field: SerializeField] public int StartDamage = 2;
    [field: SerializeField] public float Cooldown = 2f;
    [field: SerializeField] public GameObject Prefab;
    [field: SerializeField] public GameObject ExplosionPrefab;
    [field: SerializeField] public GameObject MuzzlePrefab;
}