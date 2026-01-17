using DG.Tweening;
using UnityEngine;

public class BunEnemyAnimation : IEnemyAnimationSystem
{
    private readonly Transform _bunTarget;
    
    public BunEnemyAnimation(Transform bunTarget)
    {
        _bunTarget = bunTarget;
    }
    
    public void IdleAnimation()
    {
        
    }

    public void RunAnimation()
    {
        
    }

    public void AttackAnimation()
    {
        
    }
}