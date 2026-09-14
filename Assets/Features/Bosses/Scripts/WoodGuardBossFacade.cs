using System.Collections.Generic;
using UnityEngine;

namespace Features.Bosses.Scripts
{
    [DisallowMultipleComponent]
    public sealed class WoodGuardBossFacade : BossFacade
    {
        public override Transform AttackOrigin => _attackOrigin != null ? _attackOrigin : transform;
        public Animator Animator => _animator;
        public AnimationClip AttackClip => _attackClip;
        protected override EnemyType SpawnIdentity => EnemyType.WoodGuardBoss;

        [SerializeField] private Transform _attackOrigin;
        [SerializeField] private Animator _animator;
        [SerializeField] private AnimationClip _attackClip;
        [SerializeField] private bool _showAttackPreview = true;

        private void LateUpdate() => AnimationSystem?.SetTimeScale(RelicTimeScale);

        protected override void BakeSpawnCollider()
        {
            if (Collider is MeshCollider && _animator != null && _animator.isActiveAndEnabled &&
                _animator.runtimeAnimatorController != null)
            {
                // Apply the initial Idle pose before taking the one-time collision snapshot.
                AnimatorCullingMode cullingMode = _animator.cullingMode;
                try
                {
                    _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    _animator.Update(0f);
                }
                finally
                {
                    _animator.cullingMode = cullingMode;
                }
            }
            base.BakeSpawnCollider();
        }

        private void OnDrawGizmos()
        {
            if (_showAttackPreview && Application.isPlaying == false)
                DrawAttackPreview(AttackOrigin.position, AttackRotation);
        }

        public void DrawAttackPreview(Vector3 origin, Quaternion rotation)
        {
            if (Config == null)
                return;
            var drawnAttacks = new HashSet<BossAttackConfiguration>();
            foreach (BossAttackConfiguration attack in Config.GetAllAttacks())
            {
                if (attack != null && attack.IsEnabled && drawnAttacks.Add(attack))
                    attack.DrawPreview(origin, rotation);
            }
        }
    }
}
