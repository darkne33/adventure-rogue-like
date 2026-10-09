using System;
using System.Collections.Generic;
using Features.Enemies.Scripts;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class AuraDamageArea : MonoBehaviour
{
    [SerializeField] private Transform _visualRoot;

    private readonly List<CombatTarget> _enemiesInRange = new();
    private readonly List<ParticleSystem> _particleSystems = new();
    private CharacterFacade _owner;
    private IEnemiesProvider _enemiesProvider;
    private AuraAbilityConfiguration _configuration;
    private Action<CharacterFacade, CombatTarget> _damageEnemy;
    private float _radius;
    private bool _isInitialized;

    public void Initialize(CharacterFacade owner, IEnemiesProvider enemiesProvider,
        AuraAbilityConfiguration configuration, float radius, float tickInterval,
        Action<CharacterFacade, CombatTarget> damageEnemy)
    {
        _owner = owner;
        _enemiesProvider = enemiesProvider;
        _configuration = configuration;
        _damageEnemy = damageEnemy;
        transform.localPosition = Vector3.up * _configuration.VisualHeightOffset;
        transform.localRotation = Quaternion.identity;
        SetRadius(radius);
        _isInitialized = true;

        _particleSystems.Clear();
        if (_visualRoot != null)
            _visualRoot.GetComponentsInChildren(true, _particleSystems);
        SetTickInterval(tickInterval);

        if (_visualRoot != null && _visualRoot.TryGetComponent(out ParticleSystem particles))
            particles.Play(true);
    }

    public void SetTickInterval(float tickInterval)
    {
        // A 0.5-second tick uses speed 0.5; shorter intervals speed up the existing effect.
        float simulationSpeed = 0.25f / Mathf.Max(0.0001f, tickInterval);
        foreach (ParticleSystem particles in _particleSystems)
        {
            if (particles == null)
                continue;

            ParticleSystem.MainModule main = particles.main;
            main.simulationSpeed = simulationSpeed;
        }
    }

    public void SetRadius(float radius)
    {
        _radius = Mathf.Max(0.1f, radius);
        if (_configuration == null)
            return;

        // Set the size on initialization/upgrades; parenting handles all movement.
        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        transform.localScale = new Vector3(
            1f / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
            1f / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)),
            1f / Mathf.Max(0.0001f, Mathf.Abs(parentScale.z)));

        if (_visualRoot != null)
        {
            float horizontalScale = _radius / Mathf.Max(0.01f, _configuration.VisualBaseRadius);
            _visualRoot.localScale = new Vector3(horizontalScale, _visualRoot.localScale.y, horizontalScale);
        }
    }

    // Called once per ability tick, so pause, upgrades and cooldown modifiers share one timer.
    public void DamageEnemies()
    {
        if (!_isInitialized || !isActiveAndEnabled || _owner == null || _owner.HealthSystem.IsDead)
            return;

        _enemiesInRange.Clear();
        IReadOnlyList<CombatTarget> activeEnemies = _enemiesProvider.ActiveEnemies;
        Vector3 center = _owner.transform.position;
        float radiusSqr = _radius * _radius;
        float height = Mathf.Max(0.1f, _configuration.DamageHeight);

        // Hit and kill callbacks can change the provider, so collect targets before applying damage.
        for (int index = 0; index < activeEnemies.Count; index++)
        {
            CombatTarget enemy = activeEnemies[index];
            if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.IsDead)
                continue;

            Vector3 offset = enemy.AreaDamageBounds.ClosestPoint(center) - center;
            if (Mathf.Abs(offset.y) > height)
                continue;

            offset.y = 0f;
            if (offset.sqrMagnitude <= radiusSqr)
                _enemiesInRange.Add(enemy);
        }

        for (int index = 0; index < _enemiesInRange.Count; index++)
        {
            if (!_isInitialized || _owner == null || _owner.HealthSystem.IsDead || !isActiveAndEnabled)
                break;

            CombatTarget enemy = _enemiesInRange[index];
            if (enemy != null && enemy.gameObject.activeInHierarchy && !enemy.IsDead)
                _damageEnemy(_owner, enemy);
        }

        _enemiesInRange.Clear();
    }

    private void OnDestroy()
    {
        _isInitialized = false;
        _damageEnemy = null;
        _owner = null;
        _enemiesProvider = null;
        _configuration = null;
        _enemiesInRange.Clear();
        _particleSystems.Clear();
    }
}
