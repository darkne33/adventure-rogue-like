using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using Zenject;

namespace Features.Enemies.Scripts
{
    public class EnemyFacade : MonoBehaviour
    {
        [field: SerializeField] public Transform TargetToShootDamage { get; private set; }

        public HealthSystem HealthSystem => _healthSystem;
        public DealDamageEffectSystem EffectsSystem => _effectsSystem;
        public Rigidbody Rigidbody => _rigidbody;
        public EnemyCollisionDetector EnemyCollisionDetector => _collisionDetector;
        public IEnemyAnimationSystem AnimationSystem => _animationSystem;

        internal EnemyConfiguration Configuration => _enemyConfiguration;
        internal Renderer[] MeshRenderers => _meshRenderers;

        [SerializeField] private EnemyConfiguration _enemyConfiguration;
        [SerializeField] private Renderer[] _meshRenderers;

        [Inject] private IEnemySystemsFactory _systemsFactory;

        private CharacterFacade _character;
        private Rigidbody _rigidbody;
        private NavMeshAgent _navMeshAgent;
        private EnemyCollisionDetector _collisionDetector;
        private IEnemyAnimationSystem _animationSystem;
        private IEnemyDamageSystem _damageSystem;
        private HealthSystem _healthSystem;
        private DealDamageEffectSystem _effectsSystem;

        private void Start()
        {
            _systemsFactory.Create(this);

            _navMeshAgent.speed = _enemyConfiguration.Speed;
            _navMeshAgent.angularSpeed = _enemyConfiguration.RotationSpeed;
            _navMeshAgent.updatePosition = false;
            _navMeshAgent.Warp(transform.position);

            _damageSystem.Initialize();
            _healthSystem.Initialize();
            _damageSystem.Tick(gameObject.GetCancellationTokenOnDestroy()).Forget();
        }

        private void FixedUpdate() =>
            MoveTowardsPlayer();

        internal void Construct(CharacterFacade character, Rigidbody rigidbody, NavMeshAgent navMeshAgent,
            EnemyCollisionDetector collisionDetector, IEnemyAnimationSystem animationSystem,
            IEnemyDamageSystem damageSystem, HealthSystem healthSystem, DealDamageEffectSystem effectsSystem)
        {
            _character = character;
            _rigidbody = rigidbody;
            _navMeshAgent = navMeshAgent;
            _collisionDetector = collisionDetector;
            _animationSystem = animationSystem;
            _damageSystem = damageSystem;
            _healthSystem = healthSystem;
            _effectsSystem = effectsSystem;
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

        public void SetStop(bool state) =>
            _navMeshAgent.isStopped = state;

        private void MoveTowardsPlayer()
        {
            if (_character == null || _navMeshAgent.isStopped)
                return;

            _animationSystem.RunAnimation();

            Vector3 direction = _character.transform.position - transform.position;
            direction.y = 0f;

            if (direction.magnitude <= _enemyConfiguration.DistanceToStop)
                return;

            var targetPosition = new Vector3(_character.transform.position.x, transform.position.y,
                _character.transform.position.z);

            _navMeshAgent.SetDestination(targetPosition);
            transform.position = Vector3.Lerp(
                transform.position,
                _navMeshAgent.nextPosition,
                Time.deltaTime * _navMeshAgent.speed);
        }
    }
}
