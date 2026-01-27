public abstract class CharacterActiveAbility : CharacterAbility
{
    public float Cooldown { get; protected set; }
    public float CurrentCooldown { get; set; }

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
}