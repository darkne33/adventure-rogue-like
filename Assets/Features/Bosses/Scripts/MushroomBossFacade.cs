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
        public bool IsSplitChild { get; private set; }
        public bool IsPlayingSplitTransition => _splitTransition != SplitTransition.None;
        public MushroomBossFacade SplitSibling { get; private set; }
        public float SplitSide { get; private set; } = 1f;
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
        private bool _experienceRewardClaimed;
        private SplitExperienceReward _splitExperienceReward;
        private SplitTransition _splitTransition;
        private float _splitElapsed;
        private Vector3 _splitOriginalScale;
        private float _splitRadius;
        private Vector3 _splitJumpStart;
        private Vector3 _splitJumpDestination;
        private float _splitJumpHeight;

        private enum SplitTransition { None, Inflating, Jumping }

        public override int ClaimExperienceReward()
        {
            if (!IsSplitChild)
                return base.ClaimExperienceReward();

            if (_experienceRewardClaimed || _splitExperienceReward == null || HealthSystem == null ||
                HealthSystem.CurrentHealth > 0f)
                return 0;

            _experienceRewardClaimed = true;
            return _splitExperienceReward.Claim();
        }

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
            AnimationSystem?.SetTimeScale(IsPlayingSplitTransition ? 0f : RelicTimeScale);
            if (IsPlayingSplitTransition)
            {
                UpdateSplitTransition();
                return;
            }
            if (_hasSplit || _movementConfiguration == null || HealthSystem == null || IsDead ||
                HealthSystem.CurrentHealth <= 0f)
                return;

            float threshold = Mathf.Clamp(_movementConfiguration.SplitHealthPercentage, 0f, 100f) * 0.01f;
            if (HealthSystem.CurrentHealth <= HealthSystem.MaxHealth * threshold)
                BeginSplit();
        }

        private void BeginSplit()
        {
            _hasSplit = true;
            if (_movementConfiguration.SplitPrefab == null)
            {
                Debug.LogError("Mushroom split prefab is missing in the boss configuration.", this);
                return;
            }

            _splitOriginalScale = transform.localScale;
            _splitRadius = Collider != null
                ? Mathf.Max(Collider.bounds.extents.x, Collider.bounds.extents.z)
                : _movementConfiguration.BodyRadius * SizeMultiplier;
            _splitElapsed = 0f;
            _splitTransition = SplitTransition.Inflating;
            StopCombat();
            AnimationSystem?.SetTimeScale(0f);
        }

        private void UpdateSplitTransition()
        {
            if (IsDead || HealthSystem == null || HealthSystem.CurrentHealth <= 0f)
            {
                ResetSplitTransition();
                return;
            }
            if (AttackSystem is MushroomBossAttackSystem attacks && !attacks.CanAdvanceSplitTransition)
                return;

            _splitElapsed += Time.deltaTime * RelicTimeScale;
            if (_splitTransition == SplitTransition.Inflating)
            {
                float progress = Mathf.Clamp01(_splitElapsed /
                    Mathf.Max(0.01f, _movementConfiguration.SplitInflationDuration));
                Vector3 inflation = _movementConfiguration.SplitInflationScale;
                inflation = new Vector3(Mathf.Max(1f, inflation.x), Mathf.Max(1f, inflation.y),
                    Mathf.Max(1f, inflation.z));
                // Accelerate into the burst; the stronger sideways stretch reads as a split.
                transform.localScale = Vector3.Scale(_splitOriginalScale,
                    Vector3.Lerp(Vector3.one, inflation, progress * progress));
                if (progress >= 1f)
                    Split();
                return;
            }

            float duration = Mathf.Max(0.01f, _movementConfiguration.SplitJumpDuration);
            float flight = Mathf.Clamp01(_splitElapsed / duration);
            Vector3 position = Vector3.Lerp(_splitJumpStart, _splitJumpDestination, flight);
            position.y += 4f * flight * (1f - flight) * _splitJumpHeight;
            SetSplitPosition(position);
            if (flight < 1f)
            {
                float stretch = Mathf.Sin(flight * Mathf.PI);
                transform.localScale = Vector3.Scale(_splitOriginalScale,
                    new Vector3(1f - stretch * 0.12f, 1f + stretch * 0.18f, 1f - stretch * 0.12f));
                return;
            }

            const float settleDuration = 0.6f;
            float settle = Mathf.Clamp01((_splitElapsed - duration) / settleDuration);
            float squash = Mathf.Sin(settle * Mathf.PI);
            transform.localScale = Vector3.Scale(_splitOriginalScale,
                new Vector3(1f + squash * 0.12f, 1f - squash * 0.18f, 1f + squash * 0.12f));
            if (settle >= 1f)
            {
                ResetSplitTransition();
                CombatSystem?.Start();
            }
        }

        private void BeginSplitJump(Vector3 start, Vector3 destination, float height)
        {
            _splitOriginalScale = transform.localScale;
            _splitJumpStart = start;
            _splitJumpDestination = destination;
            _splitJumpHeight = height;
            _splitElapsed = 0f;
            _splitTransition = SplitTransition.Jumping;
            SetSplitPosition(start);
            AnimationSystem?.SetTimeScale(0f);
        }

        private void SetSplitPosition(Vector3 position)
        {
            if (Rigidbody != null)
            {
                Rigidbody.position = position;
                if (!Rigidbody.isKinematic)
                {
                    Rigidbody.linearVelocity = Vector3.zero;
                    Rigidbody.angularVelocity = Vector3.zero;
                }
            }
            transform.position = position;
        }

        private void ResetSplitTransition()
        {
            if (!IsPlayingSplitTransition)
                return;
            transform.localScale = _splitOriginalScale;
            if (_splitTransition == SplitTransition.Jumping)
            {
                Vector3 position = transform.position;
                float flight = Mathf.Clamp01(_splitElapsed /
                    Mathf.Max(0.01f, _movementConfiguration.SplitJumpDuration));
                position.y = Mathf.Lerp(_splitJumpStart.y, _splitJumpDestination.y, flight);
                SetSplitPosition(position);
            }
            _splitTransition = SplitTransition.None;
            AnimationSystem?.SetTimeScale(RelicTimeScale);
        }

        protected override void OnDisable()
        {
            ResetSplitTransition();
            base.OnDisable();
        }

        private void Split()
        {
            Vector3 flashPosition = Collider != null ? Collider.bounds.center : AttackOrigin.position;
            float flashRadius = Collider != null ? Collider.bounds.extents.magnitude : _splitRadius * 1.5f;
            ResetSplitTransition();
            MushroomBossFacade prefab = _movementConfiguration.SplitPrefab;
            float scale = Mathf.Clamp(_movementConfiguration.SplitScaleMultiplier, 0.01f, 1f);
            float offset = Mathf.Max(Mathf.Max(0.05f, _splitRadius * scale) + 0.15f,
                _movementConfiguration.SplitJumpDistance * SizeMultiplier);
            Vector3 side = AttackRotation * Vector3.right;
            Vector3 childScale = transform.lossyScale * scale;
            float childHealth = HealthSystem.CurrentHealth * 0.5f;
            float childMaxHealth = HealthSystem.MaxHealth * 0.5f;
            var children = new MushroomBossFacade[2];
            var splitExperienceReward = new SplitExperienceReward(Config.Exp);

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
                    child.IsSplitChild = true;
                    child._splitExperienceReward = splitExperienceReward;
                    child.SplitSide = index == 0 ? -1f : 1f;
                    child.SizeMultiplier = SizeMultiplier * scale;
                    child.transform.localScale = childScale;
                    child.Initialize();
                    child.HealthSystem.SetMaxHealth(childMaxHealth, healIncrease: false);
                    child.HealthSystem.SetCurrentHealth(childHealth);
                    child.BeginSplitJump(transform.position, position,
                        Mathf.Max(0f, _movementConfiguration.SplitJumpHeight) * SizeMultiplier);
                }
                children[0].SplitSibling = children[1];
                children[1].SplitSibling = children[0];
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
                CombatSystem?.Start();
                return;
            }

            StopCombat();
            // Register both replacements first: removing the last enemy completes the room immediately.
            foreach (MushroomBossFacade child in children)
                _enemiesProvider.AddEnemy(child);
            _healthUI.ReplaceBoss(this, children[0], children[1]);
            _enemiesProvider.RemoveEnemy(this);
            MushroomSplitFlash.Play(flashPosition, Mathf.Max(0.1f, flashRadius));
            // Splitting is a replacement, not a kill, and must not grant the parent's death rewards.
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        private sealed class SplitExperienceReward
        {
            private readonly int _experience;
            private int _remainingBosses = 2;

            public SplitExperienceReward(int experience)
            {
                _experience = experience;
            }

            public int Claim()
            {
                if (_remainingBosses <= 0)
                    return 0;

                _remainingBosses--;
                return _remainingBosses == 0 ? _experience : 0;
            }
        }
    }
}
