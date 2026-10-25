using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using Zenject;
using Features.Sounds;

namespace Features.Enemies.Scripts
{
    public class EnemyFacade : CombatTarget
    {
        private const float NavMeshSampleDistance = 4f;
        private const float FallRecoveryDepth = 10f;
        private const float FallRecoveryHeight = 5f;
        private const float KnockbackBoundaryClearance = 0.01f;

        public override HealthSystem HealthSystem => _healthSystem;
        public override DealDamageEffectSystem EffectsSystem => _effectsSystem;
        public override Rigidbody Rigidbody => _rigidbody;
        public EnemyCollisionDetector EnemyCollisionDetector => _collisionDetector;
        public IEnemyAnimationSystem AnimationSystem => _animationSystem;
        public bool IsStopped => _navMeshAgent != null && _navMeshAgent.isActiveAndEnabled &&
                                 _navMeshAgent.isOnNavMesh ? _navMeshAgent.isStopped : _isStopped;
        public bool IsAggro { get; private set; }
        public bool IsRelicStunned => _isRelicStunned;
        internal bool IsAbilityKnockbackActive => _abilityKnockbackRemaining > 0f &&
                                                 isActiveAndEnabled && CanBeAttacked && !IsDead;
        internal bool IsAbilityKnockbackPaused
        {
            get
            {
                CharacterFacade character = _characterProvider?.CharacterFacade;
                return Time.timeScale <= 0f || RelicTimeScale <= 0f ||
                       character != null && (character.IsTransitionPaused || character.IsCinematicPaused);
            }
        }
        public bool CanAttack => _movementSystem?.CanAttack != false && _isRelicStunned == false;
        public override float RelicTimeScale => !CanBeAttacked ? 1f : _isRelicStunned
            ? 0f
            : Mathf.Clamp(_persistentRelicSlow * _temporaryRelicSlow, 0.05f, 1f);

        public EnemyConfiguration Configuration => _runtimeConfiguration != null
            ? _runtimeConfiguration : _enemyConfiguration;
        public override EnemyRank Rank => Configuration != null ? Configuration.EnemyRank : EnemyRank.Normal;
        public override Renderer[] MeshRenderers => _meshRenderers;
        // The source asset must keep hazards excluded even when a room has an older settings copy.
        public override bool CanBeAttacked => _enemyConfiguration?.IsHazard != true &&
                                             _runtimeConfiguration?.IsHazard != true;
        public override bool CountsForRoomCompletion => CanBeAttacked;
        public Transform AttackTelegraphTransform => GetAttackTelegraphTransform();
        public Transform MovementVisual => _movementVisual;
        internal ExpDropper.RoomExperienceBudget ExperienceBudget { get; private set; }
        internal double RoomExperienceShare { get; private set; }

        internal void ConfigureRoomExperience(ExpDropper.RoomExperienceBudget budget, double share)
        {
            ExperienceBudget = budget;
            RoomExperienceShare = share;
        }

        [SerializeField] private EnemyConfiguration _enemyConfiguration;
        [SerializeField] private Renderer[] _meshRenderers;
        [SerializeField] private Transform _movementVisual;
        [Inject] private ISoundsService _sounds;
        [Inject] private ICharacterProvider _characterProvider;
        public ISoundsService Sounds => _sounds;

        public void PlayCombatSound(SoundId sound) => _sounds.Play(sound);

        public void PlayAttackSound()
        {
            SoundId sound = Configuration.EnemyDamageType switch
            {
                EnemyDamageType.Melee => SoundId.EnemyMelee,
                EnemyDamageType.Dash => SoundId.EnemyDash,
                EnemyDamageType.RangeArea => SoundId.EnemyExplosion,
                EnemyDamageType.RangeBullet when SpawnType == EnemyType.Ghost => SoundId.EnemyGhostShot,
                EnemyDamageType.RangeBullet => SoundId.EnemyLavaShot,
                EnemyDamageType.RangeDirection => SoundId.EnemyNukeShot,
                EnemyDamageType.Jump => SoundId.EnemyTakeoff,
                _ => SoundId.None
            };
            _sounds.Play(sound);
        }
        private EnemyConfiguration _runtimeConfiguration;

        internal void ConfigureForRoom(EnemyHealthScalingConfiguration scaling, int roomIndex,
            ProceduralLevelSettings proceduralSettings = null, int generatedLevelNumber = 0)
        {
            _runtimeConfiguration = _enemyConfiguration.CreateForRoom(
                scaling, roomIndex, GetComponent<BombEnemySplitOnDeath>() != null,
                proceduralSettings, generatedLevelNumber);
        }

        private void OnDestroy()
        {
            _effectsSystem?.Dispose();
            if (_runtimeConfiguration != null)
                Destroy(_runtimeConfiguration);
        }

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
        private bool _isStopped;
        private Vector3 _spawnNavigationPosition;
        private Vector3 _abilityKnockbackDirection;
        private float _abilityKnockbackSpeed;
        private float _abilityKnockbackDuration;
        private float _abilityKnockbackRemaining;
        private bool _abilityKnockbackControlsVelocity;
        private bool _abilityKnockbackControlsNavigation;
        private bool _abilityKnockbackRestoreUpdatePosition;

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
            StopAbilityKnockbackVelocity();
            RefreshRelicStatuses();
            if (TryRecoverFromFall())
            {
                ClearAbilityKnockback();
                return;
            }

            if (IsDead || !CanBeAttacked)
                ClearAbilityKnockback();
            else if (IsAbilityKnockbackActive)
                ControlAbilityKnockbackNavigation();
            else
                ReleaseAbilityKnockbackNavigation();

            bool tickMovement = !IsAbilityKnockbackActive || _movementSystem is
                EnemyDashAttackSystem or EnemyJumpAttackSystem { IsExecuting: true };
            if (tickMovement && _movementSystem is not EnemyRicochetAttackSystem)
                _movementSystem.Tick();

            if (_movementSystem is not EnemyJumpAttackSystem { IsExecuting: true })
                TickAbilityKnockback();
        }

        private void OnDisable()
        {
            ClearAbilityKnockback();
        }

        private void Update()
        {
            if (_abilityKnockbackControlsNavigation)
                ControlAbilityKnockbackNavigation();

            // The NavMesh-driven hazard moves once per rendered frame, including
            // during time-slow effects. Other enemies retain their physics tick.
            if (_movementSystem is EnemyRicochetAttackSystem)
                _movementSystem.Tick();
        }

        private bool TryRecoverFromFall()
        {
            if (IsDead || transform.position.y >= _spawnNavigationPosition.y - FallRecoveryDepth)
                return false;

            Vector3 groundPosition = _spawnNavigationPosition;
            if (_navMeshAgent != null && _navMeshAgent.isActiveAndEnabled)
            {
                // Sample at floor height: the fallen body is too far below the NavMesh.
                Vector3 samplePosition = transform.position;
                samplePosition.y = _spawnNavigationPosition.y;
                if (NavMesh.SamplePosition(samplePosition, out NavMeshHit hit,
                        NavMeshSampleDistance, _navMeshAgent.areaMask))
                    groundPosition = hit.position;

                bool wasStopped = IsStopped;
                if (_navMeshAgent.Warp(groundPosition))
                {
                    _navMeshAgent.ResetPath();
                    _navMeshAgent.velocity = Vector3.zero;
                    _navMeshAgent.isStopped = wasStopped;
                }
            }

            Vector3 recoveryPosition = groundPosition + Vector3.up * FallRecoveryHeight;
            transform.position = recoveryPosition;
            if (_rigidbody != null)
            {
                _rigidbody.position = recoveryPosition;
                if (_rigidbody.isKinematic == false)
                {
                    _rigidbody.linearVelocity = Vector3.zero;
                    _rigidbody.angularVelocity = Vector3.zero;
                }
            }

            return true;
        }

        public void Construct(Rigidbody rigidbody, NavMeshAgent navMeshAgent,
            EnemyCollisionDetector collisionDetector, IEnemyAnimationSystem animationSystem,
            IEnemyMovementSystem movementSystem, IEnemyDamageSystem damageSystem,
            HealthSystem healthSystem, DealDamageEffectSystem effectsSystem,
            EnemyAggroIndicatorView aggroIndicatorView)
        {
            _spawnNavigationPosition = transform.position;
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
            if (IsStopped)
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

            _isStopped = state;
            bool hasNavigation = _navMeshAgent != null && _navMeshAgent.isActiveAndEnabled &&
                                 _navMeshAgent.isOnNavMesh;
            if (hasNavigation)
                _navMeshAgent.isStopped = state;

            if (state && hasNavigation)
            {
                if (_navMeshAgent.hasPath)
                    _navMeshAgent.ResetPath();

                _navMeshAgent.velocity = Vector3.zero;
            }

            if (state == false)
                _movementSystem.Reset();
        }

        public override void SetPersistentRelicSlow(float multiplier)
        {
            if (!CanBeAttacked)
                return;

            multiplier = Mathf.Clamp(multiplier, 0.05f, 1f);
            if (Mathf.Approximately(_persistentRelicSlow, multiplier))
                return;

            _persistentRelicSlow = multiplier;
            UpdateRelicNavigationSpeed();
        }

        public override void ApplyAbilityKnockback(Vector3 sourcePosition, float speed, float duration)
        {
            if (!isActiveAndEnabled || !CanBeAttacked || IsDead || speed <= 0f || duration <= 0f)
                return;

            Vector3 direction = transform.position - sourcePosition;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
                return;

            // A hit refreshes one short impulse, so rapid abilities cannot stack its speed.
            _abilityKnockbackDirection = direction.normalized;
            _abilityKnockbackSpeed = speed;
            _abilityKnockbackDuration = duration;
            _abilityKnockbackRemaining = duration;
            ControlAbilityKnockbackNavigation();
        }

        internal Vector3 GetAbilityKnockbackDisplacement(Vector3 position, float deltaTime)
        {
            return TryGetAbilityKnockbackStep(deltaTime, out Vector3 displacement)
                ? ConstrainAbilityKnockback(position, displacement) : Vector3.zero;
        }

        private void TickAbilityKnockback()
        {
            if (!IsAbilityKnockbackActive)
                return;

            float deltaTime = Time.fixedDeltaTime;
            TryGetAbilityKnockbackStep(deltaTime, out Vector3 displacement);

            Vector3 position = _rigidbody != null ? _rigidbody.position : transform.position;
            if (_rigidbody != null && !_rigidbody.isKinematic)
            {
                Vector3 velocity = _rigidbody.linearVelocity;
                displacement = ConstrainAbilityKnockback(position, displacement);
                // The short rebound owns horizontal movement until it ends.
                _rigidbody.linearVelocity = new Vector3(
                    displacement.x / deltaTime, velocity.y, displacement.z / deltaTime);
                _abilityKnockbackControlsVelocity = true;
                return;
            }

            displacement = ConstrainAbilityKnockback(position, displacement);
            if (_navMeshAgent != null && _navMeshAgent.isActiveAndEnabled && _navMeshAgent.isOnNavMesh)
                _navMeshAgent.Move(displacement);

            if (_rigidbody != null)
                _rigidbody.MovePosition(position + displacement);
            else
                transform.position = position + displacement;
        }

        private bool TryGetAbilityKnockbackStep(float deltaTime, out Vector3 displacement)
        {
            displacement = Vector3.zero;
            if (IsDead || !CanBeAttacked)
            {
                _abilityKnockbackRemaining = 0f;
                return false;
            }

            float scaledDeltaTime = deltaTime * RelicTimeScale;
            if (_abilityKnockbackRemaining <= 0f || scaledDeltaTime <= 0f || IsAbilityKnockbackPaused)
                return false;

            float step = Mathf.Min(scaledDeltaTime, _abilityKnockbackRemaining);
            float influence = (_abilityKnockbackRemaining - step * 0.5f) / _abilityKnockbackDuration;
            displacement = _abilityKnockbackDirection * (_abilityKnockbackSpeed * influence * step);
            _abilityKnockbackRemaining -= step;
            return true;
        }

        private Vector3 ConstrainAbilityKnockback(Vector3 position, Vector3 displacement)
        {
            if (_rigidbody != null)
            {
                if ((_rigidbody.constraints & RigidbodyConstraints.FreezePositionX) != 0)
                    displacement.x = 0f;
                if ((_rigidbody.constraints & RigidbodyConstraints.FreezePositionZ) != 0)
                    displacement.z = 0f;
            }

            float distance = displacement.magnitude;
            if (distance <= 0.0001f)
                return Vector3.zero;

            Vector3 direction = displacement / distance;
            if (_navMeshAgent != null && _navMeshAgent.isActiveAndEnabled && _navMeshAgent.isOnNavMesh)
            {
                Vector3 samplePosition = position;
                samplePosition.y = _navMeshAgent.nextPosition.y;
                if (!NavMesh.SamplePosition(samplePosition, out NavMeshHit origin,
                        Mathf.Max(0.1f, _navMeshAgent.radius), _navMeshAgent.areaMask))
                    return Vector3.zero;

                if (NavMesh.Raycast(origin.position, origin.position + displacement,
                        out NavMeshHit boundary, _navMeshAgent.areaMask))
                {
                    Vector3 offset = boundary.position - origin.position;
                    offset.y = 0f;
                    distance = Mathf.Min(distance,
                        Mathf.Max(0f, Vector3.Dot(offset, direction) - KnockbackBoundaryClearance));
                }
            }

            if (_rigidbody != null && _rigidbody.SweepTest(direction, out RaycastHit hit,
                    distance + KnockbackBoundaryClearance, QueryTriggerInteraction.Ignore))
                distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - KnockbackBoundaryClearance));

            return direction * distance;
        }

        private void StopAbilityKnockbackVelocity()
        {
            if (!_abilityKnockbackControlsVelocity)
                return;

            _abilityKnockbackControlsVelocity = false;
            if (_rigidbody == null || _rigidbody.isKinematic)
                return;

            Vector3 velocity = _rigidbody.linearVelocity;
            _rigidbody.linearVelocity = new Vector3(0f, velocity.y, 0f);
        }

        private void ControlAbilityKnockbackNavigation()
        {
            if (_navMeshAgent == null || !_navMeshAgent.isActiveAndEnabled || !_navMeshAgent.isOnNavMesh)
                return;

            if (!_abilityKnockbackControlsNavigation)
            {
                _abilityKnockbackRestoreUpdatePosition = _navMeshAgent.updatePosition;
                _abilityKnockbackControlsNavigation = true;
            }

            _navMeshAgent.updatePosition = false;
            Vector3 navigationPosition = _rigidbody != null ? _rigidbody.position : transform.position;
            navigationPosition.y = _navMeshAgent.nextPosition.y;
            _navMeshAgent.nextPosition = navigationPosition;
            _navMeshAgent.velocity = Vector3.zero;
        }

        private void ReleaseAbilityKnockbackNavigation()
        {
            if (!_abilityKnockbackControlsNavigation)
                return;

            if (_navMeshAgent != null)
            {
                if (_navMeshAgent.isActiveAndEnabled && _navMeshAgent.isOnNavMesh)
                    _navMeshAgent.nextPosition = _rigidbody != null ? _rigidbody.position : transform.position;
                _navMeshAgent.updatePosition = _abilityKnockbackRestoreUpdatePosition;
            }

            _abilityKnockbackControlsNavigation = false;
        }

        private void ClearAbilityKnockback()
        {
            _abilityKnockbackRemaining = 0f;
            StopAbilityKnockbackVelocity();
            ReleaseAbilityKnockbackNavigation();
        }

        public override void ApplyRelicSlow(float multiplier, float duration)
        {
            if (!CanBeAttacked)
                return;

            _temporaryRelicSlow = Mathf.Min(_temporaryRelicSlow, Mathf.Clamp(multiplier, 0.05f, 1f));
            _temporaryRelicSlowUntil = Mathf.Max(_temporaryRelicSlowUntil,
                Time.time + Mathf.Max(0.05f, duration));
            UpdateRelicNavigationSpeed();
        }

        public override void ApplyRelicStun(float duration)
        {
            if (!CanBeAttacked)
                return;

            if (_isRelicStunned == false)
            {
                _sounds.Play(SoundId.EnemyStun);
                _wasStoppedBeforeRelicStun = IsStopped;
                _releaseStopAfterRelicStun = false;
            }

            _relicStunnedUntil = Mathf.Max(_relicStunnedUntil, Time.time + Mathf.Max(0.05f, duration));
            _isRelicStunned = true;
            SetStop(true);

            if (_rigidbody != null && _rigidbody.isKinematic == false)
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
            if (_navMeshAgent == null || !_navMeshAgent.isActiveAndEnabled)
                return;

            Vector3 bodyPosition = _rigidbody != null ? _rigidbody.position : transform.position;
            if (NavMesh.SamplePosition(bodyPosition, out NavMeshHit hit, NavMeshSampleDistance,
                    NavMesh.AllAreas) == false || _navMeshAgent.Warp(hit.position) == false)
                return;

            // Synchronize navigation without losing the body's clearance above the floor.
            transform.position = bodyPosition;
            if (_rigidbody != null)
                _rigidbody.position = bodyPosition;
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
            _spawnNavigationPosition = navMeshPosition;

            // Stationary bosses are placed exactly at their room marker and do not need a NavMesh.
            if (_navMeshAgent == null)
                return;
            Vector3 visualPosition = transform.position;
            _navMeshAgent.speed = Configuration.Speed;
            if (Configuration.EnemyDamageType != EnemyDamageType.Ricochet)
            {
                _navMeshAgent.angularSpeed = Configuration.RotationSpeed;
                _navMeshAgent.acceleration = Configuration.Acceleration;
            }
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

            _navMeshAgent.speed = Configuration.Speed * RelicTimeScale;
        }
    }
}
