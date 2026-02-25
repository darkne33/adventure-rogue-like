using System;
using Cysharp.Threading.Tasks;
using UI;
using UnityEngine;
using UnityEngine.AI;
using Zenject;

namespace Features.Enemies.Scripts
{
    public class EnemyFacade : MonoBehaviour
    {
        public HealthSystem HealthSystem => _healthSystem;
        public EnemyEffectsSystem EffectsSystem => _effectsSystem;
        public Rigidbody Rigidbody => _rigidbody;
        public EnemyCollisionDetector EnemyCollisionDetector => _enemyCollisionDetector;

        [Inject] private ICharacterProvider _characterProvider;
        [Inject] private IEnemiesProvider _enemiesProvider;
        [Inject] private ICharacterLevelService _characterLevelService;
        [Inject] private IPanelService _panelService;

        [SerializeField] private EnemyConfiguration _enemyConfiguration;
        [SerializeField] private Renderer[] _meshRenderers;

        private Rigidbody _rigidbody;
        private NavMeshAgent _navMeshAgent;
        private Animator _animator;
        private EnemyCollisionDetector _enemyCollisionDetector;

        private IEnemyDamageSystem _enemyDamageSystem;
        private IEnemyAnimationSystem _animationSystem;
        private HealthSystem _healthSystem;
        private IHealthView _healthView;
        private EnemyEffectsSystem _effectsSystem;
        private IDeathSystem _deathSystem;

        private float _currentSpeed;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();
            _enemyCollisionDetector = GetComponent<EnemyCollisionDetector>();
        }

        private void Start()
        {
            switch (_enemyConfiguration.EnemyDamageType)
            {
                case EnemyDamageType.Melee:
                    _enemyDamageSystem = new EnemyDamageMeleeSystem(this, _characterProvider.CharacterFacade,
                        _enemyConfiguration);
                    break;
                case EnemyDamageType.Dash:
                    _enemyDamageSystem = new EnemyDashAttackSystem(_characterProvider.CharacterFacade,
                        _enemyConfiguration, this);
                    break;
                default:
                    throw new Exception("Enemy Damage Type not supported");
            }

            switch (_enemyConfiguration.EnemyAnimationType)
            {
                case EnemyAnimationType.Bun:
                    _animationSystem = new BunEnemyAnimation(_animator);
                    break;
                case EnemyAnimationType.Dummy:
                    _animationSystem = new DummyEnemyAnimation(_animator);
                    break;
                default:
                    throw new Exception("Enemy Animation Type not supported");
            }

            _navMeshAgent.speed = _enemyConfiguration.Speed;
            _navMeshAgent.angularSpeed = _enemyConfiguration.RotationSpeed;
            _enemyDamageSystem.Initialize();

            _deathSystem = new EnemyDeathSystem(_enemiesProvider, this, _characterLevelService, _enemyConfiguration);
            _healthView = GetComponent<HealthView>();
            _healthSystem = new HealthSystem(100, new[] { _healthView }, _deathSystem);
            _healthSystem.Initialize();

            _effectsSystem = new EnemyEffectsSystem(_meshRenderers);

            _enemyDamageSystem.Tick(gameObject.GetCancellationTokenOnDestroy()).Forget();
        }

        private void Update()
        {
            _animationSystem.RunAnimation();
        }

        private void FixedUpdate()
        {
            MoveTowardsPlayerNonPhysics();
        }

        public async UniTask StartDelayMovementTimer(float delay)
        {
            if (_navMeshAgent.isStopped)
                return;

            SetStop(true);
            await UniTask.Delay(TimeSpan.FromSeconds(delay),
                cancellationToken: this.GetCancellationTokenOnDestroy());
            SetStop(false);
        }

        public void SetStop(bool state) =>
            _navMeshAgent.isStopped = state;

        private void MoveTowardsPlayerNonPhysics()
        {
            if (_characterProvider.CharacterFacade == null)
                return;

            Vector3 direction = _characterProvider.CharacterFacade.transform.position - transform.position;
            direction.y = 0f;
            float distance = direction.magnitude;

            if (distance > _enemyConfiguration.DistanceToStop)
            {
                _navMeshAgent.SetDestination(new Vector3(_characterProvider.CharacterFacade.transform.position.x,
                    transform.position.y, _characterProvider.CharacterFacade.transform.position.z));
            }
        }

        private void Rotation()
        {
            var direction = transform.forward;

            if (direction.magnitude > 0.01f)
            {
                transform.forward = Vector3.Slerp(transform.forward, direction.normalized,
                    _enemyConfiguration.RotationSpeed * Time.deltaTime);
            }
        }
    }
}