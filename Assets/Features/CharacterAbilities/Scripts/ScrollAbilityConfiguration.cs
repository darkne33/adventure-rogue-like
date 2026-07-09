using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Abilities/ScrollAbilityConfiguration")]
public class ScrollAbilityConfiguration : AbilityConfiguration
{
    [field: SerializeField] public float DefaultIncreaseStat { get; private set; }
    [field: SerializeField] public string StatSuffix { get; private set; } = string.Empty;
}
