using System;
using System.Collections.Generic;
using Features.Enemies.Scripts;
using UnityEngine;

public sealed class FireFieldDamageArea : MonoBehaviour
{
    private readonly List<CombatTarget> _enemiesInRange = new();
    private readonly List<ParticleSystem> _particleSystems = new();

    private IEnemiesProvider _enemiesProvider;
    private Action<CombatTarget> _damageEnemy;
    private Action<FireFieldDamageArea> _onExpired;
    private CharacterStats _characterStats;
    private float _radiusSqr;
    private float _height;
    private float _damageTickInterval;
    private float _damageTickTimer;
    private float _remainingDuration;
    private bool _isInitialized;

    public void Initialize(IEnemiesProvider enemiesProvider, float radius, float height,
        float damageTickInterval, float duration, Action<CombatTarget> damageEnemy,
        CharacterStats characterStats = null, Action<FireFieldDamageArea> onExpired = null)
    {
        float safeRadius = Mathf.Max(0.1f, radius);
        Vector3 fieldScale = transform.lossyScale;
        float damageRadius = safeRadius * Mathf.Max(Mathf.Abs(fieldScale.x), Mathf.Abs(fieldScale.z));
        _enemiesProvider = enemiesProvider;
        _damageEnemy = damageEnemy;
        _onExpired = onExpired;
        _characterStats = characterStats;
        _radiusSqr = damageRadius * damageRadius;
        _height = Mathf.Max(0.1f, height) * Mathf.Abs(fieldScale.y);
        _damageTickInterval = Mathf.Max(0.05f, damageTickInterval);
        _damageTickTimer = 0f;
        _remainingDuration = Mathf.Max(0.1f, duration);
        ApplyParticleRadius(safeRadius);
        _isInitialized = true;
    }

    private void Update()
    {
        if (_isInitialized == false)
            return;

        _remainingDuration -= Time.deltaTime;
        if (_remainingDuration <= 0f)
        {
            NotifyExpired();
            Destroy(gameObject);
            return;
        }

        _damageTickTimer -= Time.deltaTime;
        if (_damageTickTimer > 0f)
            return;

        _damageTickTimer += Mathf.Max(0.05f, _damageTickInterval /
            Mathf.Max(0.01f, _characterStats?.RelicAttackSpeedMultiplier ?? 1f));
        DamageEnemiesInRange();
    }

    private void OnDisable() =>
        NotifyExpired();

    private void OnDestroy() =>
        NotifyExpired();

    private void NotifyExpired()
    {
        if (_isInitialized == false)
            return;

        _isInitialized = false;
        Action<FireFieldDamageArea> onExpired = _onExpired;
        _onExpired = null;
        onExpired?.Invoke(this);
    }

    private void DamageEnemiesInRange()
    {
        if (_enemiesProvider == null || _damageEnemy == null)
            return;

        _enemiesInRange.Clear();
        IReadOnlyList<CombatTarget> activeEnemies = _enemiesProvider.ActiveEnemies;
        Vector3 fieldPosition = transform.position;

        for (int index = 0; index < activeEnemies.Count; index++)
        {
            CombatTarget enemy = activeEnemies[index];
            if (enemy == null || !enemy.CanBeAttacked || enemy.gameObject.activeInHierarchy == false || enemy.IsDead)
                continue;

            Vector3 offset = enemy.AreaDamageBounds.ClosestPoint(fieldPosition) - fieldPosition;
            if (Mathf.Abs(offset.y) > _height)
                continue;

            offset.y = 0f;
            if (offset.sqrMagnitude <= _radiusSqr)
                _enemiesInRange.Add(enemy);
        }

        foreach (CombatTarget enemy in _enemiesInRange)
        {
            if (enemy != null && enemy.CanBeAttacked && enemy.IsDead == false)
                _damageEnemy(enemy);
        }
    }

    private void ApplyParticleRadius(float radius)
    {
        _particleSystems.Clear();
        GetComponentsInChildren(true, _particleSystems);

        foreach (ParticleSystem particleSystem in _particleSystems)
        {
            ParticleSystem.ShapeModule shape = particleSystem.shape;
            if (shape.enabled == false ||
                shape.shapeType is not (ParticleSystemShapeType.Circle or ParticleSystemShapeType.CircleEdge))
                continue;

            shape.radius = radius;
        }
    }
}
