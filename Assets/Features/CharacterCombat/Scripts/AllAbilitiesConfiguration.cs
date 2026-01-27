using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Abilities/AllAbilitiesConfiguration")]
public class AllAbilitiesConfiguration : ScriptableObject
{
    [field: SerializeField] public AbilityConfiguration[] Abilities { get; set; }
}