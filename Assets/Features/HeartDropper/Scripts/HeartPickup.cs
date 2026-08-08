using System;
using DG.Tweening;
using Features.Relics.Scripts;
using UnityEngine;

public sealed class HeartPickup : MonoBehaviour
{
    private HeartDropperConfiguration _configuration;
    private ICharacterProvider _characterProvider;
    private CharacterStats _characterStats;
    private RelicEventBus _relicEventBus;
    private Transform _cameraTransform;
    private Action _collectedCallback;
    private float _attractionEnabledTime;
    private Vector3 _startScale;
    private bool _collectWhenHealthFull;
    private bool _isCollecting;

    public void Construct(HeartDropperConfiguration configuration, ICharacterProvider characterProvider,
        CharacterStats characterStats, RelicEventBus relicEventBus, Transform cameraTransform,
        Vector3 landPosition, Action collectedCallback = null, bool collectWhenHealthFull = false)
    {
        _configuration = configuration;
        _characterProvider = characterProvider;
        _characterStats = characterStats;
        _relicEventBus = relicEventBus;
        _cameraTransform = cameraTransform;
        _collectedCallback = collectedCallback;
        _collectWhenHealthFull = collectWhenHealthFull;
        _attractionEnabledTime = Time.time + Mathf.Max(0f, configuration.AttractionStartDelay);
        _startScale = transform.localScale;

        name = "HeartPickup";
        PlayDropAnimation(landPosition);
    }

    private void Update()
    {
        if (_isCollecting || _configuration == null || Time.time < _attractionEnabledTime)
            return;

        CharacterFacade character = _characterProvider?.CharacterFacade;
        if (character == null || character.HealthSystem == null ||
            _collectWhenHealthFull == false && IsHealthFull(character.HealthSystem))
            return;

        Vector3 targetPosition = character.transform.position +
                                 Vector3.up * _configuration.CollectionTargetHeight;
        float distance = Vector3.Distance(transform.position, targetPosition);
        if (distance > GetAttractionRadius())
            return;

        MoveToTarget(targetPosition);
        UpdateAttractionScale(distance);

        if (distance <= _configuration.CollectDistance)
            TryCollect(character);
    }

    private void OnDestroy() =>
        transform.DOKill();

    private void PlayDropAnimation(Vector3 landPosition)
    {
        transform.DOKill();

        _ = transform.DOJump(landPosition, _configuration.DropJumpPower, 1, _configuration.DropDuration)
            .SetEase(Ease.OutQuad)
            .SetLink(gameObject);
        _ = transform.DOPunchScale(Vector3.one * 0.2f, _configuration.DropDuration, 4, 0.6f)
            .SetLink(gameObject);
    }

    private void MoveToTarget(Vector3 targetPosition)
    {
        float speedMultiplier = 1f + Mathf.Clamp01(Vector3.Distance(transform.position, targetPosition) /
                                                   Mathf.Max(0.01f, GetAttractionRadius()));
        transform.position = Vector3.MoveTowards(transform.position, targetPosition,
            _configuration.AttractionSpeed * speedMultiplier * Time.deltaTime);
    }

    private void UpdateAttractionScale(float distance)
    {
        float attractionRadius = Mathf.Max(_configuration.CollectDistance, GetAttractionRadius());
        float progress = 1f - Mathf.InverseLerp(_configuration.CollectDistance, attractionRadius, distance);
        float scaleMultiplier = Mathf.Lerp(1f, _configuration.AttractionScaleMultiplier, progress);
        transform.localScale = _startScale * scaleMultiplier;
    }

    private void TryCollect(CharacterFacade character)
    {
        if (_isCollecting)
            return;

        _isCollecting = true;
        float healAmount = character.HealthSystem.MaxHealth *
                           Mathf.Clamp01(_configuration.HealPercentage);
        float restoredHealth = character.HealthSystem.IncreaseCurrentHealth(healAmount);
        if (restoredHealth <= 0f && _collectWhenHealthFull == false)
        {
            _isCollecting = false;
            transform.localScale = _startScale;
            return;
        }

        if (restoredHealth > 0f)
        {
            _relicEventBus?.PublishHeal(new RelicHealEvent(character, restoredHealth));

            CharacterHealNumberView healNumberView = character.GetComponent<CharacterHealNumberView>();
            if (healNumberView == null)
                healNumberView = character.gameObject.AddComponent<CharacterHealNumberView>();

            healNumberView.ShowHeal(restoredHealth, _configuration.HealPopupFont, _cameraTransform);
        }

        transform.DOKill();
        Action collectedCallback = _collectedCallback;
        _collectedCallback = null;
        collectedCallback?.Invoke();
        Destroy(gameObject);
    }

    private float GetAttractionRadius()
    {
        float pickupRangeMultiplier = 1f + Mathf.Max(0f, _characterStats.PickupRange) * 0.01f;
        return _configuration.AttractionRadius * pickupRangeMultiplier;
    }

    private static bool IsHealthFull(HealthSystem healthSystem) =>
        healthSystem.CurrentHealth >= healthSystem.MaxHealth - 0.001f;
}
