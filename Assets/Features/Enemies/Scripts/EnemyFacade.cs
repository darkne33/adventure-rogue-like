using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Zenject;

namespace Features.Enemies.Scripts
{
    public class EnemyFacade : MonoBehaviour
    {
        public HealthSystem HealthSystem => _healthSystem;

        [Inject] private ICharacterProvider _characterProvider;

        [SerializeField] private EnemyConfiguration _enemyConfiguration;
        [SerializeField] private Transform _modelTarget;

        private NavMeshAgent _navMeshAgent;
        private IEnemyDamageSystem _enemyDamageSystem;
        private IEnemyAnimationSystem _animationSystem;
        private HealthSystem _healthSystem;
        private IHealthView _healthView;
        private Animator _animator;

        private float _currentSpeed;
        private bool _canMove = true;

        private void Start()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();

            switch (_enemyConfiguration.EnemyDamageType)
            {
                case EnemyDamageType.Melee:
                    _enemyDamageSystem = new EnemyDamageMeleeSystem(this, _characterProvider.CharacterFacade,
                        _enemyConfiguration, transform);
                    break;
                default:
                    throw new Exception("Enemy Damage Type not supported");
            }

            switch (_enemyConfiguration.EnemyAnimationType)
            {
                case EnemyAnimationType.Bun:
                    _animationSystem = new BunEnemyAnimation(_animator);
                    break;
                default:
                    throw new Exception("Enemy Animation Type not supported");
            }

            _navMeshAgent.speed = _enemyConfiguration.Speed;
            _navMeshAgent.angularSpeed = _enemyConfiguration.RotationSpeed;
            _enemyDamageSystem.Initialize();

            _healthView = GetComponent<HealthView>();
            _healthSystem = new HealthSystem(100, new[] { _healthView });
            _healthSystem.Initialize();
        }

        private void Update()
        {
            _animationSystem.RunAnimation();
            _enemyDamageSystem.Tick();
        }

        private void FixedUpdate()
        {
            MoveTowardsPlayerNonPhysics();
        }

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
                Rotation();
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

        public async UniTask StartDelayMovementTimer(float delay)
        {
            if (_canMove == false)
                return;

            _navMeshAgent.isStopped = true;
            _canMove = false;
            await UniTask.Delay(TimeSpan.FromSeconds(delay),
                cancellationToken: this.GetCancellationTokenOnDestroy());
            _canMove = true;
            _navMeshAgent.isStopped = false;
        }
    }
}