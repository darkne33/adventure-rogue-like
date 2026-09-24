using System;
using System.Collections.Generic;
using Features.Enemies.Scripts;
using UnityEngine;

public class PlayerCollisionDetector : MonoBehaviour
{
    public Action<CombatTarget> OnHit;

    private Transform _ignoredRoot;
    private bool _isHit;
    private Vector3 _previousPosition;
    private float _travelDistance;
    private HashSet<CombatTarget> _damagedTargets;
    public bool ManualCollisionHandling { get; set; }

    public float TravelDistance
    {
        get
        {
            RecordMovement();
            return _travelDistance;
        }
    }

    public void Initialize(Transform ignoredRoot)
    {
        _ignoredRoot = ignoredRoot;
        _previousPosition = transform.position;
        _travelDistance = 0f;
        _isHit = false;
        _damagedTargets?.Clear();
        ManualCollisionHandling = false;
    }

    private void LateUpdate() => RecordMovement();

    public int RecordDamagingHit(CombatTarget target)
    {
        _damagedTargets ??= new HashSet<CombatTarget>();
        if (target != null)
            _damagedTargets.Add(target);
        return _damagedTargets.Count;
    }

    private void RecordMovement()
    {
        Vector3 current = transform.position;
        _travelDistance += Vector3.Distance(current, _previousPosition);
        _previousPosition = current;
    }

    public bool Ignores(Collider other) =>
        other == null || other.transform == transform || other.transform.IsChildOf(transform) ||
        IsIgnored(other.transform) || IsOtherPlayerProjectile(other);

    public void ResetHit() =>
        _isHit = false;

    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other);
    }

    private void OnCollisionEnter(Collision other)
    {
        HandleHit(other.collider);
    }

    private void HandleHit(Collider other)
    {
        if (ManualCollisionHandling || _isHit || Ignores(other))
            return;

        _isHit = true;
        OnHit?.Invoke(other.GetComponentInParent<CombatTarget>());
    }

    private bool IsOtherPlayerProjectile(Collider other)
    {
        PlayerCollisionDetector otherDetector = other.GetComponentInParent<PlayerCollisionDetector>();
        return otherDetector != null && otherDetector != this;
    }

    private bool IsIgnored(Transform other)
    {
        if (_ignoredRoot == null || other == null)
            return false;

        return other == _ignoredRoot || other.IsChildOf(_ignoredRoot);
    }
}
