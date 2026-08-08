using System;
using Core;
using Features.Relics.Scripts;
using UnityEngine;
using Zenject;

public sealed class HeartDropper
{
    private readonly HeartDropperConfiguration _configuration;
    private readonly ICameraService _cameraService;
    private readonly ICharacterProvider _characterProvider;
    private readonly CharacterStats _characterStats;
    private readonly DiContainer _container;
    private readonly LevelsConfiguration _levelsConfiguration;
    private readonly RelicEventBus _relicEventBus;

    public HeartDropper(HeartDropperConfiguration configuration, ICameraService cameraService,
        ICharacterProvider characterProvider, CharacterStats characterStats, DiContainer container,
        LevelsConfiguration levelsConfiguration, RelicEventBus relicEventBus)
    {
        _configuration = configuration;
        _cameraService = cameraService;
        _characterProvider = characterProvider;
        _characterStats = characterStats;
        _container = container;
        _levelsConfiguration = levelsConfiguration;
        _relicEventBus = relicEventBus;
    }

    public bool DropHeart(Vector3 position, Action collectedCallback = null,
        bool collectWhenHealthFull = false, float additionalDropHeight = 0f)
    {
        if (_configuration == null || _configuration.HeartPrefab == null)
            return false;

        Vector3 landPosition = GetGroundedPosition(position + GetScatterOffset());
        float dropHeight = _configuration.DropHeight + Mathf.Max(0f, additionalDropHeight);
        Vector3 spawnPosition = position + Vector3.up * dropHeight;
        GameObject heartObject = _container.InstantiatePrefab(_configuration.HeartPrefab, spawnPosition,
            Quaternion.identity, null);

        HeartPickup heartPickup = heartObject.GetComponent<HeartPickup>();
        if (heartPickup == null)
            heartPickup = heartObject.AddComponent<HeartPickup>();

        heartPickup.Construct(_configuration, _characterProvider, _characterStats, _relicEventBus,
            _cameraService.MainCamera != null ? _cameraService.MainCamera.transform : null, landPosition,
            collectedCallback, collectWhenHealthFull);
        return true;
    }

    private Vector3 GetScatterOffset()
    {
        float scatterRadius = Mathf.Max(0f, _configuration.DropScatterRadius);
        if (scatterRadius <= 0f)
            return Vector3.zero;

        Vector2 offset = UnityEngine.Random.insideUnitCircle * scatterRadius;
        return new Vector3(offset.x, 0f, offset.y);
    }

    private Vector3 GetGroundedPosition(Vector3 position)
    {
        float rayStartHeight = Mathf.Max(0f, _configuration.GroundSnapRayStartHeight);
        float rayDistance = Mathf.Max(0f, _configuration.GroundSnapRayDistance);
        Vector3 rayOrigin = position + Vector3.up * rayStartHeight;

        if (rayDistance > 0f && Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit,
                rayDistance, GetGroundLayerMask(), QueryTriggerInteraction.Ignore))
            return hit.point + Vector3.up * _configuration.GroundOffset;

        return position + Vector3.up * _configuration.GroundOffset;
    }

    private LayerMask GetGroundLayerMask() =>
        _levelsConfiguration != null && _levelsConfiguration.GroundLayer.value != 0
            ? _levelsConfiguration.GroundLayer
            : Physics.DefaultRaycastLayers;
}
