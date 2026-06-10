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
        [field: SerializeField] public Transform TargetToShootDamage { get; private set; }
        public HealthSystem HealthSystem => _healthSystem;
        public DealDamageEffectSystem EffectsSystem => _effectsSystem;
        public Rigidbody Rigidbody => _rigidbody;
        public EnemyCollisionDetector EnemyCollisionDetector => _enemyCollisionDetector;
        public IEnemyAnimationSystem AnimationSystem => _animationSystem;

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
        private IDamageView _damageView;
        private DealDamageEffectSystem _effectsSystem;
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
            _navMeshAgent.updatePosition = false;
            _navMeshAgent.Warp(transform.position);

            _enemyDamageSystem.Initialize();

            _deathSystem = new EnemyDeathSystem(_enemiesProvider, this, _characterLevelService, _enemyConfiguration);
            _healthView = GetComponent<EnemyHealthView>();
            _damageView = GetComponent<EnemyDamageNumberView>();
            _healthSystem = new HealthSystem(100, new[] { _healthView }, _deathSystem, new[] { _damageView });
            _healthSystem.Initialize();

            _effectsSystem = new DealDamageEffectSystem(_meshRenderers);

            _enemyDamageSystem.Tick(gameObject.GetCancellationTokenOnDestroy()).Forget();
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
            var character = _characterProvider.CharacterFacade;
            if (character == null || _navMeshAgent.isStopped)
                return;

            _animationSystem.RunAnimation();

            Vector3 direction = character.transform.position - transform.position;
            direction.y = 0f;
            float distance = direction.magnitude;

            if (distance > _enemyConfiguration.DistanceToStop)
            {
                var targetToMove = new Vector3(character.transform.position.x, transform.position.y,
                    character.transform.position.z);

                _navMeshAgent.SetDestination(targetToMove);

                transform.position = Vector3.Lerp(
                    transform.position,
                    _navMeshAgent.nextPosition,
                    Time.deltaTime * _navMeshAgent.speed);
            }
        }
    }
}
