using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Abilities/ScrollAbilityConfiguration")]
public class ScrollAbilityConfiguration : AbilityConfiguration
{
    [field: SerializeField] public float DefaultIncreaseStat { get; private set; }
}