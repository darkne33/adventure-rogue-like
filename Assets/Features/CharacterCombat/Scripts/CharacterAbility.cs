using UnityEngine;

public abstract class CharacterAbility
{
    public AbilityName Id { get; protected set; }
    public string DisplayName { get; protected set; }
    public string Description { get; protected set; }
    public Sprite Icon { get; protected set; }

    public virtual void OnEquip(CharacterFacade character)
    {
    }

    public virtual void OnUnequip(CharacterFacade character)
    {
    }

    public virtual void Initialize(AbilityConfiguration abilityConfig)
    {
        Id = abilityConfig.AbilityName;
        DisplayName = abilityConfig.DisplayName;
        Description = abilityConfig.Description;
        Icon = abilityConfig.Icon;
    }
}