using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.Enemies.Scripts;
using UnityEngine;
using Zenject;

namespace Features.Bosses.Scripts
{
    public enum BossState
    {
        Dormant,
        Waiting,
        Attacking,
        Dead
    }

    [RequireComponent(typeof(Rigidbody))]
    public abstract class BossFacade : CombatTarget
    {
        [SerializeField] private BossConfig _bossConfig;
        [SerializeField] private Renderer[] _meshRenderers;

        private HealthSystem _healthSystem;
        private DealDamageEffectSystem _effectsSystem;
        private Rigidbody _rigidbody;
        private BossAttackSystem _attackSystem;
        private CancellationTokenSource _combatCancellation;
        private bool _initialized;
        private bool _hasStarted;
        private float _persistentSlow = 1f;
        private float _temporarySlow = 1f;
        private float _temporarySlowUntil;
        private float _stunnedUntil;

        [Inject] private ICharacterProvider _characterProvider;
        [Inject] private IEnemiesProvider _targetsProvider;
        [Inject] private CharacterStats _characterStats;
        [Inject] private GoldDropper _goldDropper;
        [Inject] private ExpDropper _expDropper;

        public BossConfig Config => _bossConfig;
        public BossState State { get; private set; } = BossState.Dormant;
        public IBossAnimationSystem AnimationSystem { get; private set; }
        public abstract Transform AttackOrigin { get; }
        public Quaternion AttackRotation => GetFlatRotation(AttackOrigin.forward);
        public override HealthSystem HealthSystem => _healthSystem;
        public override DealDamageEffectSystem EffectsSystem => _effectsSystem;
        public override Rigidbody Rigidbody => _rigidbody;
        public override Renderer[] MeshRenderers => _meshRenderers;
        public override EnemyRank Rank => EnemyRank.Boss;
        public bool CanAttack => _initialized && isActiveAndEnabled && !IsDead && RelicTimeScale > 0f;
        protected virtual EnemyType SpawnIdentity => EnemyType.None;

        public override float RelicTimeScale => Time.time < _stunnedUntil ? 0f :
            Mathf.Clamp(_persistentSlow * (Time.time < _temporarySlowUntil ? _temporarySlow : 1f), 0.05f, 1f);

        public void InitializeBoss()
        {
            if (_initialized)
                return;
            if (_bossConfig == null)
                throw new InvalidOperationException($"BossConfig is missing on {name}.");
            if (_characterProvider?.CharacterFacade == null)
                throw new InvalidOperationException($"The character is not available for boss {name}.");

            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            _rigidbody.constraints = RigidbodyConstraints.FreezeAll;
            if (!_rigidbody.isKinematic)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
            if (_meshRenderers == null || _meshRenderers.Length == 0)
                _meshRenderers = GetComponentsInChildren<Renderer>(true);

            SpawnType = SpawnIdentity;
            AnimationSystem = CreateAnimationSystem();
            _effectsSystem = new DealDamageEffectSystem(_meshRenderers);
            var deathSystem = new BossDeathSystem(this, _targetsProvider, _characterProvider.CharacterFacade,
                _characterStats, _goldDropper, _expDropper);
            _healthSystem = new HealthSystem(Mathf.Max(1, _bossConfig.MaxHealth),
                GetComponents<IHealthView>(), deathSystem, GetComponents<IDamageView>());
            _healthSystem.Initialize();
            _attackSystem = new BossAttackSystem(this, _characterProvider.CharacterFacade);
            _initialized = true;
            AnimationSystem.IdleAnimation();
        }

        protected abstract IBossAnimationSystem CreateAnimationSystem();

        protected virtual void Start()
        {
            _hasStarted = true;
            InitializeAndStartCombat(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTask InitializeAndStartCombat(CancellationToken cancellationToken)
        {
            // Scene bosses can start while the character prefab is still loading.
            if (!_initialized && _characterProvider != null)
            {
                bool wasCancelled = await UniTask.WaitUntil(
                        () => _characterProvider.CharacterFacade != null &&
                              _characterProvider.CharacterFacade.HealthSystem != null,
                        cancellationToken: cancellationToken)
                    .SuppressCancellationThrow();

                if (wasCancelled)
                    return;
            }

            InitializeBoss();
            StartCombat();
        }

        protected virtual void OnEnable()
        {
            if (_hasStarted)
                StartCombat();
        }

        protected virtual void OnDisable() => StopCombat();

        private void StartCombat()
        {
            if (!_initialized || !isActiveAndEnabled || IsDead || _combatCancellation != null)
                return;
            _attackSystem.Initialize();
            State = BossState.Waiting;
            _combatCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                this.GetCancellationTokenOnDestroy());
            RunCombat(_combatCancellation.Token).Forget();
        }

        private async UniTask RunCombat(CancellationToken cancellationToken)
        {
            try
            {
                await _attackSystem.Tick(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
        }

        public void StopCombat()
        {
            State = IsDead ? BossState.Dead : BossState.Dormant;
            CancellationTokenSource cancellation = _combatCancellation;
            _combatCancellation = null;
            cancellation?.Cancel();
            cancellation?.Dispose();
            AnimationSystem?.IdleAnimation();
        }

        internal void SetAttacking(bool attacking)
        {
            if (!IsDead && isActiveAndEnabled && State != BossState.Dormant)
                State = attacking ? BossState.Attacking : BossState.Waiting;
        }

        public override void SetPersistentRelicSlow(float multiplier) =>
            _persistentSlow = Mathf.Clamp(multiplier, 0.05f, 1f);

        public override void ApplyRelicSlow(float multiplier, float duration)
        {
            if (Time.time >= _temporarySlowUntil)
                _temporarySlow = 1f;
            _temporarySlow = Mathf.Min(_temporarySlow, Mathf.Clamp(multiplier, 0.05f, 1f));
            _temporarySlowUntil = Mathf.Max(_temporarySlowUntil, Time.time + Mathf.Max(0f, duration));
        }

        public override void ApplyRelicStun(float duration) =>
            _stunnedUntil = Mathf.Max(_stunnedUntil, Time.time + Mathf.Max(0f, duration));

        public static Quaternion GetFlatRotation(Vector3 forward)
        {
            forward.y = 0f;
            return Quaternion.LookRotation(forward.sqrMagnitude > 0.001f ? forward.normalized :
                Vector3.forward, Vector3.up);
        }
    }
}
