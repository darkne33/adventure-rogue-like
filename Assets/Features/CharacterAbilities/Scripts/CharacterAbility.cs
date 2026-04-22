using UnityEngine;

public abstract class CharacterAbility
{
    public AbilityName Id { get; protected set; }
    public string DisplayName { get; protected set; }
    public string Description { get; protected set; }
    public Sprite Icon { get; protected set; }
    public int Level { get; protected set; } = 1;

    public virtual void OnEquip(CharacterStats characterStats) => 
        Level++;

    public virtual void OnUnequip(CharacterStats characterStats) => 
        Level--;

    public virtual void Initialize(AbilityConfiguration abilityConfig)
    {
        Id = abilityConfig.AbilityName;
        DisplayName = abilityConfig.DisplayName;
        Description = abilityConfig.Description;
        Icon = abilityConfig.Icon;
    }
}