using DG.Tweening;
using UnityEngine;

public sealed class ExpRupee : MonoBehaviour
{
    private const float RotationDuration = 0.8f;
    private const float PickupEffectDestroyDelay = 1.5f;

    private ExpDropperConfiguration _configuration;
    private ICharacterProvider _characterProvider;
    private ICharacterLevelService _characterLevelService;
    private int _amount;
    private float _attractionEnabledTime;
    private float _flightStartDistance;
    private bool _isCollecting;

    public void Construct(int amount, ExpDropperConfiguration configuration,
        ICharacterProvider characterProvider, ICharacterLevelService characterLevelService,
        Vector3 burstPosition)
    {
        _amount = Mathf.Max(1, amount);
        _configuration = configuration;
        _characterProvider = characterProvider;
        _characterLevelService = characterLevelService;
        _attractionEnabledTime = Time.time + Mathf.Max(0.01f, configuration.BurstDuration) +
                                 Mathf.Max(0f, configuration.AttractionStartDelay);
        _flightStartDistance = -1f;

        name = $"ExpRupee_{_amount}";
        PlayBurstAnimation(burstPosition);
    }

    private void Update()
    {
        if (_isCollecting || _configuration == null || Time.time < _attractionEnabledTime)
            return;

        Transform character = _characterProvider?.CharacterFacade != null
            ? _characterProvider.CharacterFacade.transform
            : null;
        if (character == null)
            return;

        Vector3 targetPosition = character.position + Vector3.up * _configuration.CollectionTargetHeight;
        float distance = Vector3.Distance(transform.position, targetPosition);
        if (_flightStartDistance < 0f)
            _flightStartDistance = Mathf.Max(_configuration.CollectDistance, distance);

        MoveToTarget(targetPosition, distance);

        if (distance <= _configuration.CollectDistance)
            Collect();
    }

    private void OnDestroy() =>
        transform.DOKill();

    private void PlayBurstAnimation(Vector3 burstPosition)
    {
        transform.DOKill();

        _ = transform.DOJump(burstPosition, _configuration.BurstJumpPower, 1,
                Mathf.Max(0.01f, _configuration.BurstDuration))
            .SetEase(Ease.OutQuad)
            .SetLink(gameObject);
        _ = transform.DORotate(new Vector3(0f, 360f, 0f), RotationDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1)
            .SetLink(gameObject);
        _ = transform.DOPunchScale(Vector3.one * 0.2f, Mathf.Max(0.01f, _configuration.BurstDuration), 4, 0.6f)
            .SetLink(gameObject);
    }

    private void MoveToTarget(Vector3 targetPosition, float distance)
    {
        float speedMultiplier = 1f + Mathf.Clamp01(distance / Mathf.Max(0.01f, _flightStartDistance));
        transform.position = Vector3.MoveTowards(transform.position, targetPosition,
            _configuration.AttractionSpeed * speedMultiplier * Time.deltaTime);
    }

    private void Collect()
    {
        if (_isCollecting)
            return;

        _isCollecting = true;
        Vector3 collectPosition = transform.position;
        transform.DOKill();
        _characterLevelService.AddExp(_amount);
        SpawnPickupEffect(collectPosition);
        Destroy(gameObject);
    }

    private void SpawnPickupEffect(Vector3 position)
    {
        if (_configuration.PickupEffectPrefab == null)
            return;

        GameObject effect = Instantiate(_configuration.PickupEffectPrefab, position, Quaternion.identity);
        Destroy(effect, PickupEffectDestroyDelay);
    }
}
