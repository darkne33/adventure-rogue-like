using System;
using Features.Enemies.Scripts;
using UnityEngine;

public class PlayerCollisionDetector : MonoBehaviour
{
    public Action<EnemyFacade> OnHit;

    private Transform _ignoredRoot;
    private bool _isHit;

    public void Initialize(Transform ignoredRoot) =>
        _ignoredRoot = ignoredRoot;

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
        if (_isHit || other == null || IsIgnored(other.transform))
            return;

        _isHit = true;
        OnHit?.Invoke(other.GetComponentInParent<EnemyFacade>());
    }

    private bool IsIgnored(Transform other)
    {
        if (_ignoredRoot == null || other == null)
            return false;

        return other == _ignoredRoot || other.IsChildOf(_ignoredRoot);
    }
}
