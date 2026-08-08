using DG.Tweening;
using UI;
using UnityEngine;

public sealed class CoinGold : MonoBehaviour
{
    private const float RotationDuration = 0.8f;
    private const float PickupEffectDestroyDelay = 1.5f;

    private GoldDropperConfiguration _configuration;
    private CharacterWallet _characterWallet;
    private ICharacterProvider _characterProvider;
    private CharacterStats _characterStats;
    private IPanelService _panelService;
    private int _amount;
    private Vector3 _startScale;
    private bool _canAttract;
    private bool _isCollecting;

    public void Construct(int amount, GoldDropperConfiguration configuration, CharacterWallet characterWallet,
        ICharacterProvider characterProvider, CharacterStats characterStats, IPanelService panelService,
        Vector3 landPosition)
    {
        _amount = Mathf.Max(1, amount);
        _configuration = configuration;
        _characterWallet = characterWallet;
        _characterProvider = characterProvider;
        _characterStats = characterStats;
        _panelService = panelService;
        _startScale = transform.localScale;

        name = $"CoinGold_{_amount}";
        PlayDropAnimation(landPosition);
    }

    private void Update()
    {
        if (_isCollecting || _configuration == null || _canAttract == false)
            return;

        Transform character = _characterProvider?.CharacterFacade != null
            ? _characterProvider.CharacterFacade.transform
            : null;

        if (character == null)
            return;

        Vector3 targetPosition = GetCollectionTargetPosition(character);
        float distance = Vector3.Distance(transform.position, targetPosition);
        if (distance > GetAttractionRadius())
            return;

        MoveToTarget(targetPosition);
        UpdateAttractionScale(distance);

        if (distance <= _configuration.CollectDistance)
            Collect();
    }

    private void OnDestroy() =>
        transform.DOKill();

    private void PlayDropAnimation(Vector3 landPosition)
    {
        transform.DOKill();
        _canAttract = false;

        Vector3 startPosition = transform.position;
        float duration = Mathf.Max(0.01f, _configuration.DropDuration);
        transform.localScale = _startScale * 0.75f;

        Sequence dropSequence = DOTween.Sequence()
            .SetTarget(transform)
            .SetLink(gameObject);
        dropSequence.Append(DOVirtual.Float(0f, 1f, duration, progress =>
            {
                Vector3 position = Vector3.Lerp(startPosition, landPosition, progress);
                position.y += Mathf.Sin(progress * Mathf.PI) * _configuration.DropJumpPower;
                transform.position = position;
            })
            .SetEase(Ease.Linear));
        dropSequence.Join(transform.DOScale(_startScale, duration * 0.7f)
            .SetEase(Ease.OutBack));
        dropSequence.AppendInterval(Mathf.Max(0f, _configuration.AttractionStartDelay));
        dropSequence.OnComplete(() =>
        {
            transform.position = landPosition;
            transform.localScale = _startScale;
            _canAttract = true;
        });

        _ = transform.DORotate(new Vector3(0f, 360f, 0f), RotationDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1)
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

    private void Collect()
    {
        if (_isCollecting)
            return;

        _isCollecting = true;
        Vector3 collectPosition = transform.position;
        transform.DOKill();
        AddGold();
        SpawnPickupEffect(collectPosition);
        Destroy(gameObject);
    }

    private void SpawnPickupEffect(Vector3 position)
    {
        if (_configuration.PickupYellowPrefab == null)
            return;

        GameObject effect = Instantiate(_configuration.PickupYellowPrefab, position, Quaternion.identity);
        Destroy(effect, PickupEffectDestroyDelay);
    }

    private void AddGold()
    {
        _characterWallet.Gold.Add(_amount);

        if (_panelService?.GetPanel(PanelName.CharacterPanel) is CharacterPanel characterPanel)
            characterPanel.CharacterGoldView.ShowGold(_amount);
    }

    private Vector3 GetCollectionTargetPosition(Transform character) =>
        character.position + Vector3.up * _configuration.CollectionTargetHeight;

    private float GetAttractionRadius()
    {
        float pickupRangeMultiplier = 1f + Mathf.Max(0f, _characterStats.PickupRange) * 0.01f;
        return _configuration.AttractionRadius * pickupRangeMultiplier;
    }
}
