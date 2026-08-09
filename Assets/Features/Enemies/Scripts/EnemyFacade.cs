using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using Zenject;

namespace Features.Enemies.Scripts
{
    public class EnemyFacade : MonoBehaviour
    {
        private const float NavMeshSampleDistance = 4f;

        [field: SerializeField] public Transform TargetToShootDamage { get; private set; }

        public HealthSystem HealthSystem => _healthSystem;
        public DealDamageEffectSystem EffectsSystem => _effectsSystem;
        public Rigidbody Rigidbody => _rigidbody;
        public EnemyCollisionDetector EnemyCollisionDetector => _collisionDetector;
        public IEnemyAnimationSystem AnimationSystem => _animationSystem;
        public bool IsStopped => _navMeshAgent.isStopped;
        public bool IsDead => _healthSystem?.IsDead == true;
        public bool IsAggro { get; private set; }
        public bool CanAttack => _movementSystem?.CanAttack != false && _isRelicStunned == false;
        public float RelicTimeScale => _isRelicStunned
            ? 0f
            : Mathf.Clamp(_persistentRelicSlow * _temporaryRelicSlow, 0.05f, 1f);

        public EnemyConfiguration Configuration => _enemyConfiguration;
        public Renderer[] MeshRenderers => _meshRenderers;
        public Transform AttackTelegraphTransform => GetAttackTelegraphTransform();

        [SerializeField] private EnemyConfiguration _enemyConfiguration;
        [SerializeField] private Renderer[] _meshRenderers;

        private Rigidbody _rigidbody;
        private NavMeshAgent _navMeshAgent;
        private EnemyCollisionDetector _collisionDetector;
        private IEnemyAnimationSystem _animationSystem;
        private IEnemyMovementSystem _movementSystem;
        private IEnemyDamageSystem _damageSystem;
        private HealthSystem _healthSystem;
        private DealDamageEffectSystem _effectsSystem;
        private EnemyAggroIndicatorView _aggroIndicatorView;
        private float _persistentRelicSlow = 1f;
        private float _temporaryRelicSlow = 1f;
        private float _temporaryRelicSlowUntil;
        private float _relicStunnedUntil;
        private bool _isRelicStunned;
        private bool _wasStoppedBeforeRelicStun;
        private bool _releaseStopAfterRelicStun;

        [Inject]
        private void CreateSystems(IEnemySystemsFactory systemsFactory)
        {
            systemsFactory.Create(this);
        }

        private void Start()
        {
            _damageSystem.Initialize();
            _healthSystem.Initialize();
            _damageSystem.Tick(gameObject.GetCancellationTokenOnDestroy()).Forget();
        }

        private void FixedUpdate()
        {
            RefreshRelicStatuses();
            _movementSystem.Tick();
        }

        public void Construct(Rigidbody rigidbody, NavMeshAgent navMeshAgent,
            EnemyCollisionDetector collisionDetector, IEnemyAnimationSystem animationSystem,
            IEnemyMovementSystem movementSystem, IEnemyDamageSystem damageSystem,
            HealthSystem healthSystem, DealDamageEffectSystem effectsSystem,
            EnemyAggroIndicatorView aggroIndicatorView)
        {
            _rigidbody = rigidbody;
            _navMeshAgent = navMeshAgent;
            _collisionDetector = collisionDetector;
            _animationSystem = animationSystem;
            _movementSystem = movementSystem;
            _damageSystem = damageSystem;
            _healthSystem = healthSystem;
            _effectsSystem = effectsSystem;
            _aggroIndicatorView = aggroIndicatorView;
        }

        public async UniTask StartDelayMovementTimer(float delay)
        {
            if (_navMeshAgent.isStopped)
                return;

            SetStop(true);
            await UniTask.Delay(TimeSpan.FromSeconds(delay),
                cancellationToken: gameObject.GetCancellationTokenOnDestroy());
            SetStop(false);
        }

        public void SetStop(bool state)
        {
            if (state == false && IsDead)
                return;

            if (state == false && _isRelicStunned)
            {
                _releaseStopAfterRelicStun = true;
                return;
            }

            _navMeshAgent.isStopped = state;

            if (state && _navMeshAgent.isOnNavMesh)
            {
                if (_navMeshAgent.hasPath)
                    _navMeshAgent.ResetPath();

                _navMeshAgent.velocity = Vector3.zero;
            }

            if (state == false)
                _movementSystem.Reset();
        }

        public void SetPersistentRelicSlow(float multiplier)
        {
            multiplier = Mathf.Clamp(multiplier, 0.05f, 1f);
            if (Mathf.Approximately(_persistentRelicSlow, multiplier))
                return;

            _persistentRelicSlow = multiplier;
            UpdateRelicNavigationSpeed();
        }

        public void ApplyRelicSlow(float multiplier, float duration)
        {
            _temporaryRelicSlow = Mathf.Min(_temporaryRelicSlow, Mathf.Clamp(multiplier, 0.05f, 1f));
            _temporaryRelicSlowUntil = Mathf.Max(_temporaryRelicSlowUntil,
                Time.time + Mathf.Max(0.05f, duration));
            UpdateRelicNavigationSpeed();
        }

        public void ApplyRelicStun(float duration)
        {
            if (_isRelicStunned == false)
            {
                _wasStoppedBeforeRelicStun = IsStopped;
                _releaseStopAfterRelicStun = false;
            }

            _relicStunnedUntil = Mathf.Max(_relicStunnedUntil, Time.time + Mathf.Max(0.05f, duration));
            _isRelicStunned = true;
            SetStop(true);

            if (_rigidbody != null)
            {
                Vector3 velocity = _rigidbody.linearVelocity;
                velocity.x = 0f;
                velocity.z = 0f;
                _rigidbody.linearVelocity = velocity;
            }

            UpdateRelicNavigationSpeed();
        }

        public void SyncNavigationPosition()
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, NavMeshSampleDistance,
                    NavMesh.AllAreas))
                _navMeshAgent.Warp(hit.position);
        }

        public void NotifyAttackFinished() =>
            _movementSystem.OnAttackFinished();

        private Transform GetAttackTelegraphTransform()
        {
            if (_meshRenderers == null)
                return transform;

            for (int i = 0; i < _meshRenderers.Length; i++)
            {
                Renderer meshRenderer = _meshRenderers[i];
                if (meshRenderer == null)
                    continue;

                Transform visualRoot = meshRenderer.transform;
                if (visualRoot != transform && visualRoot.IsChildOf(transform) == false)
                    return transform;

                while (visualRoot != transform && visualRoot.parent != transform)
                    visualRoot = visualRoot.parent;

                return visualRoot;
            }

            return transform;
        }

        internal void ActivateAggro()
        {
            if (IsAggro)
                return;

            IsAggro = true;
            PlayAggroReaction().Forget();
        }

        private async UniTask PlayAggroReaction()
        {
            float reactionDuration = Mathf.Max(0f, _enemyConfiguration.AggroReactionDuration);

            SetStop(true);
            _animationSystem.IdleAnimation();
            _aggroIndicatorView?.Play(reactionDuration);

            bool wasCancelled = await UniTask.Delay(
                    TimeSpan.FromSeconds(reactionDuration),
                    cancellationToken: gameObject.GetCancellationTokenOnDestroy())
                .SuppressCancellationThrow();

            if (wasCancelled == false && this != null)
                SetStop(false);
        }

        internal void InitializeNavigation(Vector3 navMeshPosition)
        {
            Vector3 visualPosition = transform.position;
            _navMeshAgent.speed = _enemyConfiguration.Speed;
            _navMeshAgent.angularSpeed = _enemyConfiguration.RotationSpeed;
            _navMeshAgent.acceleration = _enemyConfiguration.Acceleration;
            _navMeshAgent.updatePosition = false;

            if (_navMeshAgent.Warp(navMeshPosition) == false)
                throw new InvalidOperationException(
                    $"Enemy {name} could not be placed on NavMesh at {navMeshPosition}.");

            transform.position = visualPosition;
            UpdateRelicNavigationSpeed();
        }

        private void RefreshRelicStatuses()
        {
            bool speedChanged = false;

            if (_temporaryRelicSlow < 1f && Time.time >= _temporaryRelicSlowUntil)
            {
                _temporaryRelicSlow = 1f;
                speedChanged = true;
            }

            if (_isRelicStunned && Time.time >= _relicStunnedUntil)
            {
                bool shouldReleaseStop = _releaseStopAfterRelicStun || _wasStoppedBeforeRelicStun == false;
                _isRelicStunned = false;
                _releaseStopAfterRelicStun = false;
                _wasStoppedBeforeRelicStun = false;
                if (shouldReleaseStop)
                    SetStop(false);
                speedChanged = true;
            }

            if (speedChanged)
                UpdateRelicNavigationSpeed();
        }

        private void UpdateRelicNavigationSpeed()
        {
            if (_navMeshAgent == null || _enemyConfiguration == null)
                return;

            _navMeshAgent.speed = _enemyConfiguration.Speed * RelicTimeScale;
        }
    }
}
