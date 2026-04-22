using UnityEngine;

public abstract class AbilityConfiguration : ScriptableObject
{
    [field: SerializeField] public AbilityName AbilityName { get; private set; }
    [field: SerializeField] public string DisplayName = "Ability!";
    [field: SerializeField] public string Description = "Oops it's ABILITY?! :)";
    [field: SerializeField]  public Sprite Icon { get; private set; }
}