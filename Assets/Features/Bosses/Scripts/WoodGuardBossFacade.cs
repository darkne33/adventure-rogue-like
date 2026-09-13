using UnityEngine;

namespace Features.Bosses.Scripts
{
    [DisallowMultipleComponent]
    public sealed class WoodGuardBossFacade : BossFacade
    {
        [SerializeField] private Transform _attackOrigin;
        [SerializeField] private Animator _animator;
        [SerializeField] private AnimationClip _attackClip;
        [SerializeField] private bool _showAttackPreview = true;

        public override Transform AttackOrigin => _attackOrigin != null ? _attackOrigin : transform;
        protected override EnemyType SpawnIdentity => EnemyType.WoodGuardBoss;

        protected override IBossAnimationSystem CreateAnimationSystem() =>
            new WoodGuardBossAnimation(_animator, _attackClip);

        private void OnDrawGizmos()
        {
            if (_showAttackPreview && Application.isPlaying == false)
                DrawAttackPreview(AttackOrigin.position, AttackRotation);
        }

        public void DrawAttackPreview(Vector3 origin, Quaternion rotation)
        {
            if (Config == null || Config.Attacks == null)
                return;
            foreach (BossAttackConfiguration attack in Config.Attacks)
            {
                if (attack != null && attack.IsEnabled)
                    attack.DrawPreview(origin, rotation);
            }
        }
    }
}
