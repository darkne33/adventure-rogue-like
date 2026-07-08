using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Abilities/RabbitBoomerangAbilityConfiguration")]
public class RabbitBoomerangAbilityConfiguration : ShootableAbilityConfiguration
{
    [field: SerializeField] public int StartTargets { get; private set; } = 1;
    [field: Header("Upgrades")]
    [field: SerializeField, Min(0f)] public float DamageUpgradeIncrease { get; private set; } = 15f;
    [field: SerializeField, Min(0f)] public float SpeedUpgradeIncrease { get; private set; } = 5f;
    [field: SerializeField, Min(0f)] public float CooldownUpgradeReduction { get; private set; } = 0.2f;
    [field: SerializeField, Min(0f)] public int TargetUpgradeIncrease { get; private set; } = 1;
    [field: SerializeField, Min(0f)] public float BounceRadiusUpgradeIncrease { get; private set; } = 2.4f;
    [field: SerializeField] public float BounceRadius { get; private set; } = 12f;
    [field: SerializeField] public float OvertravelDistance { get; private set; } = 8f;
    [field: SerializeField] public float RaycastSkin { get; private set; } = 0.05f;
}
