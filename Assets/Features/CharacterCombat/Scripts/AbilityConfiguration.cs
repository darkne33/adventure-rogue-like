using UnityEngine;

public abstract class AbilityConfiguration : ScriptableObject
{
    [field: SerializeField] public AbilityName AbilityName { get; private set; }
    [field: SerializeField] public string DisplayName = "Fireball";
    [field: SerializeField] public string Description = "Shoots a fireball!:)";
    [field: SerializeField]  public Sprite Icon { get; private set; }
}