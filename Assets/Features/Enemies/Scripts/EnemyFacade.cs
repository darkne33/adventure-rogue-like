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

        public EnemyConfiguration Configuration => _enemyConfiguration;
        public Renderer[] MeshRenderers => _meshRenderers;

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

        private void FixedUpdate() =>
            _movementSystem.Tick();

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

            _navMeshAgent.isStopped = state;

            if (state == false)
                _movementSystem.Reset();
        }

        public void SyncNavigationPosition()
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, NavMeshSampleDistance,
                    NavMesh.AllAreas))
                _navMeshAgent.Warp(hit.position);
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
        }
    }
}
