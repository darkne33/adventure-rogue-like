using UnityEngine;

namespace Features.Bosses.Scripts
{
    [DisallowMultipleComponent]
    public sealed class OgreBossFacade : BossFacade
    {
        public override Transform AttackOrigin => _attackOrigin != null ? _attackOrigin : transform;
        public Transform ImpactOrigin => _impactOrigin != null ? _impactOrigin : AttackOrigin;
        public Animator Animator => _animator;
        public AnimationClip AttackClip => _attackClip;
        public OgreBossConfiguration MovementConfiguration => _movementConfiguration;
        protected override EnemyType SpawnIdentity => EnemyType.OgreBoss;

        [SerializeField] private Animator _animator;
        [SerializeField] private AnimationClip _attackClip;
        [SerializeField] private OgreBossConfiguration _movementConfiguration;
        [SerializeField] private Transform _attackOrigin;
        [Tooltip("Impact marker outside the animated skeleton. Its XZ position fixes the warning and damage area.")]
        [SerializeField] private Transform _impactOrigin;
    }
}
