using UnityEngine;

namespace Features.Enemies.Scripts
{
    public sealed class GhostEnemyAnimation : IEnemyAnimationSystem
    {
        private static readonly int IsRunning = Animator.StringToHash("IsRunning");
        private static readonly int Attack = Animator.StringToHash("Attack");

        private readonly Animator _animator;

        public GhostEnemyAnimation(Animator animator)
        {
            _animator = animator;
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
}