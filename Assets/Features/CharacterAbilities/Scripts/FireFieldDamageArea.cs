using System;
using System.Collections.Generic;
using DG.Tweening;
using Features.Enemies.Scripts;
using UnityEngine;

public sealed class FireFieldDamageArea : MonoBehaviour
{
    [SerializeField] private Transform _puddleVisual;
    [SerializeField, Min(0.01f)] private float _puddleBaseDiameter = 2f;
    [SerializeField, Range(0f, 1f)] private float _puddleSpawnScale = 0.15f;
    [SerializeField, Min(0f)] private float _puddleSpreadDuration = 0.35f;

    private readonly List<EnemyFacade> _enemiesInRange = new();
    private readonly List<ParticleSystem> _particleSystems = new();

    private IEnemiesProvider _enemiesProvider;
    private Action<EnemyFacade> _damageEnemy;
    private float _radiusSqr;
    private float _height;
    private float _damageTickInterval;
    private float _damageTickTimer;
    private float _remainingDuration;
    private bool _isInitialized;
    private Tween _puddleSpreadTween;

    public void Initialize(IEnemiesProvider enemiesProvider, float radius, float height,
        float damageTickInterval, float duration, Action<EnemyFacade> damageEnemy)
    {
        float safeRadius = Mathf.Max(0.1f, radius);
        _enemiesProvider = enemiesProvider;
        _damageEnemy = damageEnemy;
        _radiusSqr = safeRadius * safeRadius;
        _height = Mathf.Max(0.1f, height);
        _damageTickInterval = Mathf.Max(0.05f, damageTickInterval);
        _damageTickTimer = 0f;
        _remainingDuration = Mathf.Max(0.1f, duration);
        ApplyParticleRadius(safeRadius);
        ApplyPuddleRadius(safeRadius);
        _isInitialized = true;
    }

    private void Update()
    {
        if (_isInitialized == false)
            return;

        _remainingDuration -= Time.deltaTime;
        if (_remainingDuration <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        _damageTickTimer -= Time.deltaTime;
        if (_damageTickTimer > 0f)
            return;

        _damageTickTimer += _damageTickInterval;
        DamageEnemiesInRange();
    }

    private void DamageEnemiesInRange()
    {
        if (_enemiesProvider == null || _damageEnemy == null)
            return;

        _enemiesInRange.Clear();
        IReadOnlyList<EnemyFacade> activeEnemies = _enemiesProvider.ActiveEnemies;
        Vector3 fieldPosition = transform.position;

        for (int index = 0; index < activeEnemies.Count; index++)
        {
            EnemyFacade enemy = activeEnemies[index];
            if (enemy == null || enemy.gameObject.activeInHierarchy == false || enemy.IsDead)
                continue;

            Vector3 offset = enemy.transform.position - fieldPosition;
            if (Mathf.Abs(offset.y) > _height)
                continue;

            offset.y = 0f;
            if (offset.sqrMagnitude <= _radiusSqr)
                _enemiesInRange.Add(enemy);
        }

        foreach (EnemyFacade enemy in _enemiesInRange)
        {
            if (enemy != null && enemy.IsDead == false)
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

    private void ApplyPuddleRadius(float radius)
    {
        if (_puddleVisual == null)
            return;

        float scale = radius * 2f / Mathf.Max(0.01f, _puddleBaseDiameter);
        Vector3 targetScale = Vector3.one * scale;

        _puddleSpreadTween?.Kill();

        if (_puddleSpreadDuration <= 0f)
        {
            _puddleVisual.localScale = targetScale;
            return;
        }

        _puddleVisual.localScale = targetScale * _puddleSpawnScale;
        _puddleSpreadTween = _puddleVisual
            .DOScale(targetScale, _puddleSpreadDuration)
            .SetEase(Ease.OutCubic)
            .SetLink(gameObject);
    }
}
