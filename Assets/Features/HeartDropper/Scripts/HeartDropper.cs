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

    public void TryDropHeart(Vector3 position)
    {
        if (_configuration == null || _configuration.HeartPrefab == null ||
            Random.value >= Mathf.Clamp01(_configuration.DropChance))
            return;

        Vector3 landPosition = GetGroundedPosition(position + GetScatterOffset());
        Vector3 spawnPosition = landPosition + Vector3.up * _configuration.DropHeight;
        GameObject heartObject = _container.InstantiatePrefab(_configuration.HeartPrefab, spawnPosition,
            Quaternion.identity, null);

        HeartPickup heartPickup = heartObject.GetComponent<HeartPickup>();
        if (heartPickup == null)
            heartPickup = heartObject.AddComponent<HeartPickup>();

        heartPickup.Construct(_configuration, _characterProvider, _characterStats, _relicEventBus,
            _cameraService.MainCamera != null ? _cameraService.MainCamera.transform : null, landPosition);
    }

    private Vector3 GetScatterOffset()
    {
        float scatterRadius = Mathf.Max(0f, _configuration.DropScatterRadius);
        if (scatterRadius <= 0f)
            return Vector3.zero;

        Vector2 offset = Random.insideUnitCircle * scatterRadius;
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
