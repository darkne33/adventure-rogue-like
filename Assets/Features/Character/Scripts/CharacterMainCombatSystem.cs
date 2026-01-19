using UnityEngine;

public class CharacterMainCombatSystem : ICombatSystem
{
    private readonly Transform _enemyTarget;
    
    private float _attackDelay = 1f;

    public CharacterMainCombatSystem()
    {
        
    }
    
    public void Attack()
    {
        
    }
}

public interface ICombatSystem
{
    void Attack();
}