using UnityEngine;

namespace Features.Bosses.Scripts
{
    [DisallowMultipleComponent]
    public sealed class MushroomBossFacade : BossFacade
    {
        public override Transform AttackOrigin => _attackOrigin != null ? _attackOrigin : transform;
        public Animator Animator => _animator;
        public AnimationClip JumpClip => _jumpClip;
        public AnimationClip HeadClip => _headClip;
        public MushroomBossConfiguration MovementConfiguration => _movementConfiguration;
        public Transform LandingEffectOrigin => _landingEffectOrigin != null ? _landingEffectOrigin : AttackOrigin;
        public Transform HeadImpactOrigin => _headImpactOrigin != null ? _headImpactOrigin : AttackOrigin;
        protected override EnemyType SpawnIdentity => EnemyType.MushroomBoss;

        [SerializeField] private Animator _animator;
        [SerializeField] private AnimationClip _jumpClip;
        [SerializeField] private AnimationClip _headClip;
        [SerializeField] private MushroomBossConfiguration _movementConfiguration;
        [SerializeField] private Transform _attackOrigin;
        [Tooltip("Ground-level spawn point for SparkleNovaRed at landing.")]
        [SerializeField] private Transform _landingEffectOrigin;
        [Tooltip("Ground-level head impact point, captured when the head attack starts. Keep it outside the animated skeleton.")]
        [SerializeField] private Transform _headImpactOrigin;

        private void LateUpdate() => AnimationSystem?.SetTimeScale(RelicTimeScale);
    }
}
