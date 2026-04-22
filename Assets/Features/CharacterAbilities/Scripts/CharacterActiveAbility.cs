public abstract class CharacterActiveAbility : CharacterAbility
{
    public float Cooldown { get; protected set; }
    public float CurrentCooldown { get; set; }
    
    public float Stat_1 { get; protected set; }
    public string StatName_1 { get; protected set; }
    
    public float Stat_2 { get; protected set; }
    public string StatName_2 { get; protected set; }

    public virtual bool CanUse(CharacterFacade character) => 
        CurrentCooldown <= 0f && IsReady(character);

    protected virtual bool IsReady(CharacterFacade character) => true;

    public virtual void Use(CharacterFacade character)
    {
        if (!CanUse(character)) 
            return;
        
        OnUse(character);
        CurrentCooldown = Cooldown;
    }

    protected abstract void OnUse(CharacterFacade character);
    
    public abstract float GetStatFromIncrease();
    public abstract float GetStatToIncrease();
}