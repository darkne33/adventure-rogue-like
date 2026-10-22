using UnityEngine;

public abstract class AbilityConfiguration : ScriptableObject
{
    [field: SerializeField] public AbilityName AbilityName { get; private set; }
    [field: SerializeField] public bool IsDisabled { get; private set; }
    [field: SerializeField] public string DisplayName = "Ability!";
    [field: SerializeField] public string Description = "Oops it's ABILITY?! :)";
    [field: SerializeField]  public Sprite Icon { get; private set; }

    [field: Header("Hit knockback")]
    [field: SerializeField, Min(0f)]
    [field: UnityEngine.Serialization.FormerlySerializedAs("<Knockback>k__BackingField")]
    [field: UnityEngine.Serialization.FormerlySerializedAs("<StartKnockback>k__BackingField")]
    public float HitKnockbackSpeed { get; private set; } = 4f;
    [field: SerializeField, Min(0f)] public float HitKnockbackDuration { get; private set; } = 0.15f;
}
