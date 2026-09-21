using System;
using Features.Bosses.UI;
using Features.Enemies.Scripts;
using UnityEngine;
using Zenject;

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
        public float SizeMultiplier { get; private set; } = 1f;
        protected override EnemyType SpawnIdentity => EnemyType.MushroomBoss;

        [SerializeField] private Animator _animator;
        [SerializeField] private AnimationClip _jumpClip;
        [SerializeField] private AnimationClip _headClip;
        [SerializeField] private MushroomBossConfiguration _movementConfiguration;
        [SerializeField] private Transform _attackOrigin;
        [Tooltip("Exact world-space spawn position for SparkleNovaRed at landing, including height.")]
        [SerializeField] private Transform _landingEffectOrigin;
        [Tooltip("The captured head impact point uses ImpactDistance for XZ and preserves this marker's authored height for NovaRed. Keep it outside the animated skeleton.")]
        [SerializeField] private Transform _headImpactOrigin;

        [Inject] private IBossFactory _bossFactory;
        [Inject] private IEnemiesProvider _enemiesProvider;
        [Inject] private BossHealthUIController _healthUI;

        private bool _hasSplit;

        public void SetHeadImpactPoint(Vector3 position)
        {
            if (_headImpactOrigin != null)
            {
                position.y = _headImpactOrigin.position.y;
                _headImpactOrigin.position = position;
            }
        }

        private void LateUpdate()
        {
            AnimationSystem?.SetTimeScale(RelicTimeScale);
            if (_hasSplit || _movementConfiguration == null || HealthSystem == null || IsDead ||
                HealthSystem.CurrentHealth <= 0f)
                return;

            float threshold = Mathf.Clamp(_movementConfiguration.SplitHealthPercentage, 0f, 100f) * 0.01f;
            if (HealthSystem.CurrentHealth <= HealthSystem.MaxHealth * threshold)
                Split();
        }

        private void Split()
        {
            _hasSplit = true;
            MushroomBossFacade prefab = _movementConfiguration.SplitPrefab;
            if (prefab == null)
            {
                Debug.LogError("Mushroom split prefab is missing in the boss configuration.", this);
                return;
            }

            float scale = Mathf.Clamp(_movementConfiguration.SplitScaleMultiplier, 0.01f, 1f);
            float radius = Collider != null
                ? Mathf.Max(Collider.bounds.extents.x, Collider.bounds.extents.z)
                : _movementConfiguration.BodyRadius * SizeMultiplier;
            float offset = Mathf.Max(0.05f, radius * scale) + 0.15f;
            Vector3 side = AttackRotation * Vector3.right;
            Vector3 childScale = transform.lossyScale * scale;
            float childHealth = HealthSystem.CurrentHealth * 0.5f;
            float childMaxHealth = HealthSystem.MaxHealth * 0.5f;
            var children = new MushroomBossFacade[2];

            try
            {
                for (int index = 0; index < children.Length; index++)
                {
                    Vector3 direction = index == 0 ? -side : side;
                    Vector3 position = AttackSystem is MushroomBossAttackSystem attacks
                        ? attacks.GetSplitPosition(direction, offset)
                        : transform.position;
                    MushroomBossFacade child = (MushroomBossFacade)_bossFactory.Create(
                        prefab, position, transform.rotation);
                    children[index] = child;
                    // Set before initialization so neither child can split again, even below 50% HP.
                    child._hasSplit = true;
                    child.SizeMultiplier = SizeMultiplier * scale;
                    child.transform.localScale = childScale;
                    child.Initialize();
                    child.HealthSystem.SetMaxHealth(childMaxHealth, healIncrease: false);
                    child.HealthSystem.SetCurrentHealth(childHealth);
                }
            }
            catch (Exception exception)
            {
                foreach (MushroomBossFacade child in children)
                {
                    if (child != null)
                    {
                        child.gameObject.SetActive(false);
                        Destroy(child.gameObject);
                    }
                }
                Debug.LogException(exception, this);
                return;
            }

            StopCombat();
            // Register both replacements first: removing the last enemy completes the room immediately.
            foreach (MushroomBossFacade child in children)
                _enemiesProvider.AddEnemy(child);
            _healthUI.ReplaceBoss(this, children[0], children[1]);
            _enemiesProvider.RemoveEnemy(this);
            // Splitting is a replacement, not a kill, and must not grant the parent's death rewards.
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
