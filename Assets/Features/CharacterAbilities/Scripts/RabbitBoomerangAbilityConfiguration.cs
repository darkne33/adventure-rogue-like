using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Abilities/RabbitBoomerangAbilityConfiguration")]
public class RabbitBoomerangAbilityConfiguration : ShootableAbilityConfiguration
{
    [field: SerializeField] public int StartTargets { get; private set; } = 1;
    [field: SerializeField] public int TargetsPerLevel { get; private set; } = 1;
    [field: SerializeField] public float BounceRadius { get; private set; } = 12f;
    [field: SerializeField] public float OvertravelDistance { get; private set; } = 8f;
    [field: SerializeField] public float RaycastSkin { get; private set; } = 0.05f;
}
