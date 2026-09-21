using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.Enemies.Scripts;
using UnityEngine;
using Zenject;

namespace Features.Bosses.Scripts
{
    [RequireComponent(typeof(Rigidbody))]
    public abstract class BossFacade : CombatTarget
    {
        public override HealthSystem HealthSystem => _healthSystem;
        public override DealDamageEffectSystem EffectsSystem => _effectsSystem;
        public override Rigidbody Rigidbody => _rigidbody;
        public Collider Collider => _collider;
        public override Renderer[] MeshRenderers => _meshRenderers;
        public override EnemyRank Rank => EnemyRank.Boss;
        public IBossAnimationSystem AnimationSystem => _animationSystem;
        public BossAttackSystem AttackSystem => _attackSystem;
        public BossCombatSystem CombatSystem => _combatSystem;
        public BossConfig Config => _bossConfig;
        public BossState State => _combatSystem?.State ?? BossState.Dormant;
        public abstract Transform AttackOrigin { get; }
        public Quaternion AttackRotation => GetFlatRotation(AttackOrigin.forward);
        public bool CanAttack => _initialized && isActiveAndEnabled && !IsDead && RelicTimeScale > 0f;
        public override float RelicTimeScale => Time.time < _stunnedUntil ? 0f :
            Mathf.Clamp(_persistentSlow * (Time.time < _temporarySlowUntil ? _temporarySlow : 1f), 0.05f, 1f);

        protected virtual EnemyType SpawnIdentity => EnemyType.None;

        [SerializeField] private BossConfig _bossConfig;
        [SerializeField] private Collider _collider;
        [SerializeField] private Renderer[] _meshRenderers;
        [Tooltip("Projectile aim points, selected in order. Empty entries are skipped. " +
                 "Uses Target To Shoot Damage when no points are assigned.")]
        [SerializeField] private Transform[] _targetsToShootDamage = Array.Empty<Transform>();

        [Inject] private IBossSystemsFactory _systemsFactory;
        [Inject] private ICharacterProvider _characterProvider;

        private Rigidbody _rigidbody;
        private Mesh _spawnColliderMesh;
        private HealthSystem _healthSystem;
        private DealDamageEffectSystem _effectsSystem;
        private IBossAnimationSystem _animationSystem;
        private BossAttackSystem _attackSystem;
        private BossCombatSystem _combatSystem;
        private bool _initialized;
        private bool _hasStarted;
        private float _persistentSlow = 1f;
        private float _temporarySlow = 1f;
        private float _temporarySlowUntil;
        private float _stunnedUntil;
        private int _nextProjectileTargetIndex;

        protected virtual void Start()
        {
            _hasStarted = true;
            InitializeAndStartCombat(this.GetCancellationTokenOnDestroy()).Forget();
        }

        protected virtual void OnEnable()
        {
            if (_hasStarted && _initialized)
                _combatSystem.Start();
        }

        protected virtual void OnDisable() => StopCombat();

        protected virtual void OnDestroy()
        {
            _combatSystem?.Dispose();
            _effectsSystem?.Dispose();
            if (_spawnColliderMesh != null)
                Destroy(_spawnColliderMesh);
        }

        public void Initialize()
        {
            if (_initialized)
                return;

            _systemsFactory.Create(this);
            _rigidbody.useGravity = false;
            _rigidbody.constraints = RigidbodyConstraints.FreezeAll;
            if (!_rigidbody.isKinematic)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
            SpawnType = SpawnIdentity;
            _healthSystem.Initialize();
            _initialized = true;
            _animationSystem.IdleAnimation();
            BakeSpawnCollider();
        }

        protected virtual void BakeSpawnCollider()
        {
            if (_collider is not MeshCollider meshCollider)
                return;

            SkinnedMeshRenderer source = null;
            foreach (SkinnedMeshRenderer renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.sharedMesh == null)
                    continue;

                source ??= renderer;
                if (renderer.sharedMesh == meshCollider.sharedMesh)
                {
                    source = renderer;
                    break;
                }
            }
            if (source == null)
                return;

            Mesh originalMesh = meshCollider.sharedMesh;
            var bakedMesh = new Mesh { name = $"{name}_SpawnCollider" };
            try
            {
                // Compensate renderer scale; it is applied by the collider transform below.
                source.BakeMesh(bakedMesh, true);
                if (bakedMesh.vertexCount < 3 || bakedMesh.triangles.Length < 3)
                    throw new InvalidOperationException("The baked mesh contains no collision geometry.");

                // The renderer and collider may be on different transforms.
                Matrix4x4 rendererToCollider = meshCollider.transform.worldToLocalMatrix *
                                              source.transform.localToWorldMatrix;
                Vector3[] vertices = bakedMesh.vertices;
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = rendererToCollider.MultiplyPoint3x4(vertices[i]);
                    float magnitude = vertices[i].sqrMagnitude;
                    if (float.IsNaN(magnitude) || float.IsInfinity(magnitude))
                        throw new InvalidOperationException("The baked mesh contains invalid vertices.");
                }
                bakedMesh.vertices = vertices;
                bakedMesh.RecalculateBounds();
                if (bakedMesh.bounds.size.sqrMagnitude <= Mathf.Epsilon)
                    throw new InvalidOperationException("The baked mesh has zero size.");

                meshCollider.sharedMesh = bakedMesh;
                if (meshCollider.enabled && meshCollider.gameObject.activeInHierarchy &&
                    meshCollider.bounds.size.sqrMagnitude <= Mathf.Epsilon)
                    throw new InvalidOperationException("Physics could not create the baked collider.");

                _spawnColliderMesh = bakedMesh;
            }
            catch (Exception exception)
            {
                meshCollider.sharedMesh = originalMesh;
                Debug.LogWarning($"Could not bake the spawn collider for '{name}'. " +
                                 $"The original mesh was kept. {exception.Message}", this);
            }
            finally
            {
                if (_spawnColliderMesh != bakedMesh)
                    Destroy(bakedMesh);
            }
        }

        public void Construct(Rigidbody rigidbody, Renderer[] meshRenderers, HealthSystem healthSystem,
            IBossAnimationSystem animationSystem, BossAttackSystem attackSystem,
            BossCombatSystem combatSystem, DealDamageEffectSystem effectsSystem)
        {
            _rigidbody = rigidbody;
            _meshRenderers = meshRenderers;
            _healthSystem = healthSystem;
            _animationSystem = animationSystem;
            _attackSystem = attackSystem;
            _combatSystem = combatSystem;
            _effectsSystem = effectsSystem;
        }

        public void StopCombat() => _combatSystem?.Stop();

        public virtual int ClaimExperienceReward() => Mathf.Max(0, Config.Exp);

        public Transform GetClosestProjectileTarget(Vector3 position)
        {
            Transform closestTarget = null;
            float closestSqrDistance = float.PositiveInfinity;

            if (_targetsToShootDamage != null)
            {
                foreach (Transform target in _targetsToShootDamage)
                {
                    if (target == null)
                        continue;

                    float sqrDistance = (target.position - position).sqrMagnitude;
                    if (sqrDistance >= closestSqrDistance)
                        continue;

                    closestSqrDistance = sqrDistance;
                    closestTarget = target;
                }
            }

            return closestTarget != null ? closestTarget : base.GetNextProjectileTarget();
        }

        public override Transform GetNextProjectileTarget()
        {
            if (_targetsToShootDamage != null && _targetsToShootDamage.Length > 0)
            {
                for (int offset = 0; offset < _targetsToShootDamage.Length; offset++)
                {
                    int index = (_nextProjectileTargetIndex + offset) % _targetsToShootDamage.Length;
                    Transform target = _targetsToShootDamage[index];
                    if (target == null)
                        continue;

                    _nextProjectileTargetIndex = (index + 1) % _targetsToShootDamage.Length;
                    return target;
                }
            }

            return base.GetNextProjectileTarget();
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

            Initialize();
            _combatSystem.Start();
        }
    }
}
