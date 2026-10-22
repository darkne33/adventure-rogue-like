using System.Collections.Generic;
using UnityEngine;

// All renderers and visual objects come from the prepared Aegis prefabs.
// The ability supplies the gameplay clock, so the orbit and waves pause together.
[DisallowMultipleComponent]
public sealed class AegisShieldView : MonoBehaviour
{
    [SerializeField] private Transform _orbitRoot;
    [SerializeField] private Transform _shieldTemplate;
    [SerializeField] private AegisBlockBurstView _wavePrefab;
    [SerializeField, Min(0.1f)] private float _orbitRadius = 1.6f;
    [SerializeField] private float _orbitDegreesPerSecond = 55f;
    [SerializeField] private Vector3 _shieldRotationOffset;
    [SerializeField, Min(0f)] private float _waveHeightOffset = 0.1f;

    private readonly List<ShieldSlot> _shields = new();
    private readonly List<BlockWave> _waves = new();
    private readonly float[] _sweepPartitions = new float[8];
    private CharacterFacade _owner;
    private Vector3 _ownerLocalAnchor;
    private int _capacity;
    private float _orbitAngle;
    private float _sizeMultiplier = 1f;
    private float _rotationDegreesPerSecond;

    public void Initialize(CharacterFacade owner)
    {
        Cleanup();
        _owner = owner;
        _ownerLocalAnchor = GetOwnerLocalAnchor(owner);
        _orbitAngle = 0f;
        _rotationDegreesPerSecond = _orbitDegreesPerSecond;
        UpdatePose();
    }

    public void Configure(float sizeMultiplier, float rotationDegreesPerSecond)
    {
        _sizeMultiplier = Mathf.Max(0.01f, sizeMultiplier);
        _rotationDegreesPerSecond = Mathf.Max(0f, rotationDegreesPerSecond);
        foreach (ShieldSlot shield in _shields)
            shield.Transform.localScale = shield.BaseScale * _sizeMultiplier;
        UpdatePose();
        ArrangeShields();
    }

    public void SetShieldSlots(int capacity, IReadOnlyList<bool> activeSlots)
    {
        _capacity = Mathf.Max(0, capacity);
        if (_orbitRoot == null || _shieldTemplate == null)
            return;

        while (_shields.Count < _capacity)
        {
            Transform shield = Instantiate(_shieldTemplate, _orbitRoot, false);
            ShieldSlot slot = new(shield, shield.GetComponent<BoxCollider>());
            shield.localScale = slot.BaseScale * _sizeMultiplier;
            _shields.Add(slot);
        }

        for (int index = 0; index < _shields.Count; index++)
            _shields[index].Transform.gameObject.SetActive(index < _capacity &&
                activeSlots != null && index < activeSlots.Count && activeSlots[index]);

        UpdatePose();
        ArrangeShields();
    }

    public void Tick(float deltaTime)
    {
        if (_owner == null || _owner.HealthSystem.IsDead)
        {
            if (_orbitRoot != null)
                _orbitRoot.gameObject.SetActive(false);
            CleanupWaves();
            return;
        }

        float elapsed = Mathf.Max(0f, deltaTime);
        _orbitAngle = Mathf.Repeat(_orbitAngle + _rotationDegreesPerSecond * elapsed, 360f);
        UpdatePose();
        ArrangeShields();

        for (int index = _waves.Count - 1; index >= 0; index--)
        {
            BlockWave wave = _waves[index];
            wave.Elapsed += elapsed;
            if (wave.View == null || wave.Elapsed >= wave.Duration)
            {
                if (wave.View != null)
                    Destroy(wave.View.gameObject);
                _waves.RemoveAt(index);
                continue;
            }

            float progress = Mathf.Clamp01(wave.Elapsed / wave.Duration);
            wave.View.SetProgress(progress);
        }
    }

    public bool TryIntersectSegment(Vector3 from, Vector3 to, float projectileRadius,
        out int slot, out Vector3 hitPoint, out float hitDistance)
    {
        slot = -1;
        hitPoint = default;
        hitDistance = 0f;
        if (!PrepareGeometry())
            return false;

        float nearestFraction = float.PositiveInfinity;
        float radius = Mathf.Max(0f, projectileRadius);
        for (int index = 0; index < _capacity; index++)
        {
            if (!TryGetActiveBox(index, out BoxCollider collider))
                continue;

            BoxShape box = new(collider);
            Quaternion inverseRotation = Quaternion.Inverse(box.Rotation);
            Vector3 localFrom = inverseRotation * (from - box.Center);
            Vector3 localTo = inverseRotation * (to - box.Center);
            if (!SweepSphereAgainstBox(localFrom, localTo, box.Extents, radius, out float fraction) ||
                fraction >= nearestFraction)
                continue;

            nearestFraction = fraction;
            slot = index;
            Vector3 localContact = ClosestPointOnBox(Vector3.Lerp(localFrom, localTo, fraction), box.Extents);
            hitPoint = box.Center + box.Rotation * localContact;
        }

        if (slot < 0)
            return false;

        hitDistance = Vector3.Distance(from, to) * nearestFraction;
        return true;
    }

    public bool TryIntersectSphere(Vector3 center, float radius, out int slot, out Vector3 hitPoint)
    {
        slot = -1;
        hitPoint = default;
        if (!PrepareGeometry())
            return false;

        float radiusSquared = Mathf.Max(0f, radius) * Mathf.Max(0f, radius);
        float closestSquared = float.PositiveInfinity;
        for (int index = 0; index < _capacity; index++)
        {
            if (!TryGetActiveBox(index, out BoxCollider collider))
                continue;

            BoxShape box = new(collider);
            Vector3 localCenter = Quaternion.Inverse(box.Rotation) * (center - box.Center);
            Vector3 closest = ClosestPointOnBox(localCenter, box.Extents);
            float distanceSquared = (localCenter - closest).sqrMagnitude;
            if (distanceSquared > radiusSquared || distanceSquared >= closestSquared)
                continue;

            closestSquared = distanceSquared;
            slot = index;
            hitPoint = box.Center + box.Rotation * closest;
        }
        return slot >= 0;
    }

    public bool TryIntersectCollider(Collider other, out int slot, out Vector3 hitPoint)
    {
        slot = -1;
        hitPoint = default;
        if (!PrepareGeometry() || other == null || !other.enabled || !other.gameObject.activeInHierarchy ||
            other.transform.IsChildOf(_owner.transform))
            return false;

        float closestSquared = float.PositiveInfinity;
        for (int index = 0; index < _capacity; index++)
        {
            if (!TryGetActiveBox(index, out BoxCollider collider))
                continue;

            // The authored shield collider stays disabled in the broadphase. This explicit
            // overlap query uses its geometry and current pose without registering a trigger.
            if (!Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation,
                    other, other.transform.position, other.transform.rotation, out _, out _))
                continue;

            BoxShape box = new(collider);
            Vector3 otherPoint = other.ClosestPoint(box.Center);
            Vector3 localPoint = Quaternion.Inverse(box.Rotation) * (otherPoint - box.Center);
            Vector3 contact = box.Center + box.Rotation * ClosestPointOnBox(localPoint, box.Extents);
            float distanceSquared = (contact - otherPoint).sqrMagnitude;
            if (distanceSquared >= closestSquared)
                continue;
            closestSquared = distanceSquared;
            slot = index;
            hitPoint = contact;
        }
        return slot >= 0;
    }

    public bool TryIntersectBox(Vector3 center, Vector3 halfExtents, Quaternion rotation,
        out int slot, out Vector3 hitPoint)
    {
        slot = -1;
        hitPoint = default;
        if (!PrepareGeometry())
            return false;

        BoxShape attackBox = new(center, halfExtents, rotation);
        float closestSquared = float.PositiveInfinity;
        for (int index = 0; index < _capacity; index++)
        {
            if (!TryGetActiveBox(index, out BoxCollider collider))
                continue;
            BoxShape shieldBox = new(collider);
            if (!BoxesOverlap(shieldBox, attackBox))
                continue;

            Vector3 localCenter = Quaternion.Inverse(shieldBox.Rotation) * (center - shieldBox.Center);
            Vector3 contact = shieldBox.Center + shieldBox.Rotation *
                ClosestPointOnBox(localCenter, shieldBox.Extents);
            float distanceSquared = (contact - center).sqrMagnitude;
            if (distanceSquared >= closestSquared)
                continue;
            closestSquared = distanceSquared;
            slot = index;
            hitPoint = contact;
        }
        return slot >= 0;
    }

    public void PlayBlock(Vector3 direction, float radius, float duration, float arcDegrees = 120f)
    {
        if (_owner == null || _wavePrefab == null)
            return;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            direction = _owner.transform.forward;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.forward;

        AegisBlockBurstView view = Instantiate(_wavePrefab,
            _owner.ProjectileSpawnPosition + Vector3.up * _waveHeightOffset,
            Quaternion.LookRotation(direction.normalized, Vector3.up));
        view.Configure(radius, arcDegrees);
        _waves.Add(new BlockWave(view, Mathf.Max(0.01f, duration)));
    }

    public void Cleanup()
    {
        CleanupWaves();
        foreach (ShieldSlot slot in _shields)
        {
            Transform shield = slot.Transform;
            if (shield != null)
            {
                shield.gameObject.SetActive(false);
                Destroy(shield.gameObject);
            }
        }
        _shields.Clear();
        _capacity = 0;
        _owner = null;
    }

    private void UpdatePose()
    {
        if (_owner == null)
            return;

        // Inherit the interpolated character Transform instead of snapping back to
        // Collider.bounds, which follows the discrete physics pose while moving.
        if (transform.parent == _owner.transform)
        {
            transform.localPosition = _ownerLocalAnchor;
            transform.rotation = Quaternion.identity;
        }
        else
        {
            transform.SetPositionAndRotation(_owner.transform.TransformPoint(_ownerLocalAnchor),
                Quaternion.identity);
        }
        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        transform.localScale = new Vector3(
            1f / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
            1f / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)),
            1f / Mathf.Max(0.0001f, Mathf.Abs(parentScale.z)));
        if (_orbitRoot != null)
            _orbitRoot.gameObject.SetActive(true);
    }

    private static Vector3 GetOwnerLocalAnchor(CharacterFacade owner)
    {
        if (owner == null || owner.Collider == null)
            return Vector3.zero;

        Collider body = owner.Collider;
        Vector3 worldCenter = body switch
        {
            BoxCollider box => box.transform.TransformPoint(box.center),
            SphereCollider sphere => sphere.transform.TransformPoint(sphere.center),
            CapsuleCollider capsule => capsule.transform.TransformPoint(capsule.center),
            _ => body.bounds.center
        };
        return owner.transform.InverseTransformPoint(worldCenter);
    }

    private void ArrangeShields()
    {
        for (int index = 0; index < _capacity && index < _shields.Count; index++)
        {
            // Slot angles depend on capacity, never on the number of unbroken shields.
            float angle = (_orbitAngle + index * 360f / _capacity) * Mathf.Deg2Rad;
            Vector3 outward = new(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            Transform shield = _shields[index].Transform;
            shield.localPosition = outward * _orbitRadius;
            shield.localRotation = Quaternion.LookRotation(outward, Vector3.up) *
                                   Quaternion.Euler(_shieldRotationOffset);
        }
    }

    private bool CanIntersect() =>
        isActiveAndEnabled && _owner != null && _owner.isActiveAndEnabled &&
        _owner.HealthSystem != null && !_owner.HealthSystem.IsDead;

    private bool PrepareGeometry()
    {
        if (!CanIntersect())
            return false;
        // A projectile can query us before this frame's ability tick. Follow current
        // character movement without advancing the orbit or rebuilding slot angles.
        UpdatePose();
        ArrangeShields();
        return true;
    }

    private bool TryGetActiveBox(int index, out BoxCollider collider)
    {
        collider = null;
        if (index < 0 || index >= _shields.Count)
            return false;
        ShieldSlot shield = _shields[index];
        if (shield.Transform == null || !shield.Transform.gameObject.activeInHierarchy)
            return false;
        collider = shield.Collider;
        return collider != null;
    }

    private bool SweepSphereAgainstBox(Vector3 from, Vector3 to, Vector3 extents,
        float radius, out float fraction)
    {
        const float squaredTolerance = 0.00000001f;
        fraction = 0f;
        float radiusSquared = radius * radius;
        if ((from - ClosestPointOnBox(from, extents)).sqrMagnitude <= radiusSquared + squaredTolerance)
            return true;

        Vector3 delta = to - from;
        int partitionCount = 2;
        _sweepPartitions[0] = 0f;
        _sweepPartitions[1] = 1f;
        for (int axis = 0; axis < 3; axis++)
        {
            if (Mathf.Abs(delta[axis]) <= 0.0000001f)
                continue;
            for (int sign = -1; sign <= 1; sign += 2)
            {
                float crossing = (sign * extents[axis] - from[axis]) / delta[axis];
                if (crossing > 0f && crossing < 1f)
                    _sweepPartitions[partitionCount++] = crossing;
            }
        }

        // At most six face crossings; insertion sort avoids per-projectile allocations.
        for (int index = 1; index < partitionCount; index++)
        {
            float value = _sweepPartitions[index];
            int insertion = index - 1;
            while (insertion >= 0 && _sweepPartitions[insertion] > value)
            {
                _sweepPartitions[insertion + 1] = _sweepPartitions[insertion];
                insertion--;
            }
            _sweepPartitions[insertion + 1] = value;
        }

        // Squared distance to an OBB is quadratic between face crossings. Solving
        // each interval includes the rounded edges/corners of a sphere sweep,
        // rather than treating a merely expanded box as a collision.
        for (int interval = 0; interval < partitionCount - 1; interval++)
        {
            float lower = _sweepPartitions[interval];
            float upper = _sweepPartitions[interval + 1];
            Vector3 atLower = from + delta * lower;
            if ((atLower - ClosestPointOnBox(atLower, extents)).sqrMagnitude <=
                radiusSquared + squaredTolerance)
            {
                fraction = lower;
                return true;
            }

            Vector3 midpoint = from + delta * ((lower + upper) * 0.5f);
            double coefficientA = 0d;
            double coefficientB = 0d;
            double coefficientC = -(double)radiusSquared;
            for (int axis = 0; axis < 3; axis++)
            {
                if (Mathf.Abs(midpoint[axis]) <= extents[axis])
                    continue;
                double offset = from[axis] - (midpoint[axis] < 0f ? -extents[axis] : extents[axis]);
                double movement = delta[axis];
                coefficientA += movement * movement;
                coefficientB += 2d * offset * movement;
                coefficientC += offset * offset;
            }

            if (coefficientA <= 0.00000000000001d)
                continue;
            double discriminant = coefficientB * coefficientB - 4d * coefficientA * coefficientC;
            if (discriminant < 0d)
                continue;
            double root = (-coefficientB - System.Math.Sqrt(discriminant)) / (2d * coefficientA);
            if (root < lower - 0.000001d || root > upper + 0.000001d)
                continue;
            fraction = Mathf.Clamp((float)root, lower, upper);
            return true;
        }
        return false;
    }

    private static Vector3 ClosestPointOnBox(Vector3 point, Vector3 extents) =>
        new(Mathf.Clamp(point.x, -extents.x, extents.x),
            Mathf.Clamp(point.y, -extents.y, extents.y),
            Mathf.Clamp(point.z, -extents.z, extents.z));

    private static bool BoxesOverlap(BoxShape first, BoxShape second)
    {
        // The separating-axis theorem tests both boxes' face normals and all
        // nine edge cross products. No temporary collider or broadphase query.
        for (int axis = 0; axis < 3; axis++)
        {
            Vector3 firstAxis = first.GetAxis(axis);
            if (!OverlapsOnAxis(first, second, firstAxis) ||
                !OverlapsOnAxis(first, second, second.GetAxis(axis)))
                return false;
            for (int otherAxis = 0; otherAxis < 3; otherAxis++)
            {
                if (!OverlapsOnAxis(first, second,
                        Vector3.Cross(firstAxis, second.GetAxis(otherAxis))))
                    return false;
            }
        }
        return true;
    }

    private static bool OverlapsOnAxis(BoxShape first, BoxShape second, Vector3 axis)
    {
        float axisSquared = axis.sqrMagnitude;
        if (axisSquared < 0.000000000001f)
            return true;

        float firstRadius = 0f;
        float secondRadius = 0f;
        for (int component = 0; component < 3; component++)
        {
            firstRadius += first.Extents[component] * Mathf.Abs(Vector3.Dot(first.GetAxis(component), axis));
            secondRadius += second.Extents[component] * Mathf.Abs(Vector3.Dot(second.GetAxis(component), axis));
        }
        float centerDistance = Mathf.Abs(Vector3.Dot(second.Center - first.Center, axis));
        return centerDistance <= firstRadius + secondRadius + 0.000001f * Mathf.Sqrt(axisSquared);
    }

    private void CleanupWaves()
    {
        foreach (BlockWave wave in _waves)
        {
            if (wave.View != null)
                Destroy(wave.View.gameObject);
        }
        _waves.Clear();
    }

    private void OnDisable() => CleanupWaves();
    private void OnDestroy() => Cleanup();

    private sealed class ShieldSlot
    {
        public ShieldSlot(Transform transform, BoxCollider collider)
        {
            Transform = transform;
            Collider = collider;
            BaseScale = transform.localScale;
        }

        public Transform Transform { get; }
        public BoxCollider Collider { get; }
        public Vector3 BaseScale { get; }
    }

    private readonly struct BoxShape
    {
        public BoxShape(BoxCollider collider)
        {
            Transform shapeTransform = collider.transform;
            Center = shapeTransform.TransformPoint(collider.center);
            Rotation = shapeTransform.rotation;
            Vector3 scale = shapeTransform.lossyScale;
            Extents = Vector3.Scale(collider.size * 0.5f,
                new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        }

        public BoxShape(Vector3 center, Vector3 extents, Quaternion rotation)
        {
            Center = center;
            Rotation = rotation;
            Extents = new Vector3(Mathf.Abs(extents.x), Mathf.Abs(extents.y), Mathf.Abs(extents.z));
        }

        public Vector3 Center { get; }
        public Quaternion Rotation { get; }
        public Vector3 Extents { get; }

        public Vector3 GetAxis(int axis) =>
            Rotation * (axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward);
    }

    private sealed class BlockWave
    {
        public BlockWave(AegisBlockBurstView view, float duration)
        {
            View = view;
            Duration = duration;
        }

        public AegisBlockBurstView View { get; }
        public float Duration { get; }
        public float Elapsed { get; set; }
    }
}
