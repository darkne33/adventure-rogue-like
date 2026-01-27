using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Abilities/Fireball")]
public class FireballAbilityConfiguration : AbilityConfiguration
{
    [field: SerializeField] public int StartDamage = 2;
    [field: SerializeField] public float Cooldown = 2f;
    [field: SerializeField] public GameObject Prefab;
}