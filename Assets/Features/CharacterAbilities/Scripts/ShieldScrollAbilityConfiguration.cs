using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Abilities/ShieldScrollAbilityConfiguration")]
public sealed class ShieldScrollAbilityConfiguration : ScrollAbilityConfiguration
{
    [field: SerializeField] public Color ShieldColor { get; private set; } = Color.blue;
}
