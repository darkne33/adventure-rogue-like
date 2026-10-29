using UnityEngine;

namespace Features.Bosses.Scripts
{
    [DisallowMultipleComponent]
    public sealed class GolemBossFacade : BossFacade
    {
        public override Transform AttackOrigin => _attackOrigin != null ? _attackOrigin : transform;
        public Transform ImpactOrigin => _impactOrigin != null ? _impactOrigin : AttackOrigin;
        public Animator Animator => _animator;
        public AnimationClip AttackClip => _attackClip;
        public GolemBossConfiguration MovementConfiguration => _movementConfiguration;
        protected override EnemyType SpawnIdentity => EnemyType.GolemBoss;

        [SerializeField] private Animator _animator;
        [SerializeField] private AnimationClip _attackClip;
        [SerializeField] private GolemBossConfiguration _movementConfiguration;
        [SerializeField] private Transform _attackOrigin;
        [Tooltip("Ground-level impact marker outside the animated skeleton, centered under the boss's feet.")]
        [SerializeField] private Transform _impactOrigin;
    }
}
