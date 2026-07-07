using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Abilities/FireballAbilityConfiguration")]
public class FireballAbilityConfiguration : ShootableAbilityConfiguration
{
    [field: SerializeField] public float TravelDistance { get; private set; } = 100f;
}
