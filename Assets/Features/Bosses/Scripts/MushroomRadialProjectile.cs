using UnityEngine;

namespace Features.Bosses.Scripts
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
    public sealed class MushroomRadialProjectile : MonoBehaviour
    {
        [SerializeField] private SphereCollider _hitCollider;
        [SerializeField] private LayerMask _blockingLayers;
        [SerializeField] private LayerMask _characterLayers;

        private readonly RaycastHit[] _castHits = new RaycastHit[16];
        private readonly Collider[] _overlaps = new Collider[16];
        private BossFacade _owner;
        private CharacterFacade _character;
        private ParticleSystem[] _particles;
        private Vector3 _direction;
        private float _speed;
        private float _lifetime;
        private float _damage;
        private float _age;
        private bool _launched;
        private bool _resolved;
        private bool _visualsPaused;

        private void Awake() => _particles = GetComponentsInChildren<ParticleSystem>(true);

        public void Launch(BossFacade owner, CharacterFacade character, Vector3 direction,
            float speed, float lifetime, float damage)
        {
            _owner = owner;
            _character = character;
            direction.y = 0f;
            _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            transform.rotation = Quaternion.LookRotation(_direction);
            _speed = Mathf.Max(0.1f, speed);
            _lifetime = Mathf.Max(0.1f, lifetime);
            _damage = Mathf.Max(0f, damage);
            _age = 0f;
            _resolved = false;
            _launched = true;

            if (_owner == null || _character == null || _hitCollider == null)
            {
                Debug.LogError("Boss radial projectile requires an owner, character and hit collider.", this);
                Resolve(null);
                return;
            }

            // Validate the whole outward spawn offset, including thin walls before the rim.
            Vector3 origin = _owner.RadialAttackOrigin.position;
            Vector3 spawnOffset = transform.position - origin;
            float spawnDistance = spawnOffset.magnitude;
            float radius = GetWorldRadius();
            if (IsSpawnBlocked(origin, radius, spawnOffset, spawnDistance))
            {
                Resolve(null);
                return;
            }

            foreach (ParticleSystem particles in _particles)
            {
                particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                particles.Play(false);
            }
        }

        private bool IsSpawnBlocked(Vector3 origin, float radius, Vector3 offset, float distance)
        {
            foreach (Collider collider in Physics.OverlapSphere(origin, radius, _blockingLayers,
                         QueryTriggerInteraction.Ignore))
            {
                if (IsRelevantHit(collider))
                    return true;
            }
            if (distance <= 0.0001f)
                return false;
            foreach (RaycastHit hit in Physics.SphereCastAll(origin, radius, offset / distance,
                         distance, _blockingLayers, QueryTriggerInteraction.Ignore))
            {
                if (IsRelevantHit(hit.collider))
                    return true;
            }
            return false;
        }

        private void Update()
        {
            if (!_launched || _resolved)
                return;

            if (_owner == null || !_owner.isActiveAndEnabled || _owner.IsDead ||
                _owner is MushroomBossFacade { IsPlayingSplitTransition: true } || _character == null ||
                !_character.isActiveAndEnabled || _character.HealthSystem == null ||
                _character.HealthSystem.IsDead || _character.IsTransitionPaused || _hitCollider == null)
            {
                Resolve(null);
                return;
            }

            bool paused = Time.deltaTime <= 0f || _character.IsCinematicPaused;
            SetVisualsPaused(paused);
            if (paused)
                return;

            Vector3 center = _hitCollider.transform.TransformPoint(_hitCollider.center);
            float radius = GetWorldRadius();
            int mask = _blockingLayers.value | _characterLayers.value;
            int overlapCount = Physics.OverlapSphereNonAlloc(center, radius, _overlaps, mask,
                QueryTriggerInteraction.Collide);
            Collider[] overlaps = overlapCount == _overlaps.Length
                ? Physics.OverlapSphere(center, radius, mask, QueryTriggerInteraction.Collide)
                : _overlaps;
            if (overlapCount == _overlaps.Length)
                overlapCount = overlaps.Length;
            for (int i = 0; i < overlapCount; i++)
            {
                if (IsRelevantHit(overlaps[i]))
                {
                    Resolve(overlaps[i]);
                    return;
                }
            }

            float delta = Mathf.Min(Time.deltaTime, _lifetime - _age);
            float distance = _speed * delta;
            int hitCount = Physics.SphereCastNonAlloc(center, radius, _direction, _castHits,
                distance, mask, QueryTriggerInteraction.Collide);
            RaycastHit[] hits = hitCount == _castHits.Length
                ? Physics.SphereCastAll(center, radius, _direction, distance, mask,
                    QueryTriggerInteraction.Collide)
                : _castHits;
            if (hitCount == _castHits.Length)
                hitCount = hits.Length;

            Collider nearestCollider = null;
            float nearestDistance = distance;
            for (int i = 0; i < hitCount; i++)
            {
                if (hits[i].distance <= nearestDistance && IsRelevantHit(hits[i].collider))
                {
                    nearestCollider = hits[i].collider;
                    nearestDistance = hits[i].distance;
                }
            }

            transform.position += _direction * nearestDistance;
            if (nearestCollider != null)
            {
                Resolve(nearestCollider);
                return;
            }

            _age += delta;
            if (_age >= _lifetime)
                Resolve(null);
        }

        private float GetWorldRadius()
        {
            Vector3 scale = _hitCollider.transform.lossyScale;
            return _hitCollider.radius * Mathf.Max(Mathf.Abs(scale.x),
                Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        }

        private bool IsRelevantHit(Collider other)
        {
            if (other == null || other == _hitCollider || other.transform.IsChildOf(_owner.transform))
                return false;
            if (other.GetComponentInParent<CharacterFacade>() == _character)
                return true;
            if (other.isTrigger || other.GetComponentInParent<CombatTarget>() != null)
                return false;
            return (_blockingLayers.value & (1 << other.gameObject.layer)) != 0;
        }

        private void Resolve(Collider hit)
        {
            if (_resolved)
                return;
            _resolved = true;
            _launched = false;
            if (_hitCollider != null)
                _hitCollider.enabled = false;
            if (hit != null && _character != null &&
                hit.GetComponentInParent<CharacterFacade>() == _character)
                _character.ReceiveDamage(_damage, _owner);
            Destroy(gameObject);
        }

        private void SetVisualsPaused(bool paused)
        {
            if (_visualsPaused == paused)
                return;
            _visualsPaused = paused;
            foreach (ParticleSystem particles in _particles)
            {
                if (paused)
                    particles.Pause(false);
                else
                    particles.Play(false);
            }
        }
    }
}
