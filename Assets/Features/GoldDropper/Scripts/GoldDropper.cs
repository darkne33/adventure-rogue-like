using UI;
using UnityEngine;
using Zenject;

public class GoldDropper
{
    private readonly GoldDropperConfiguration _configuration;
    private readonly CharacterWallet _characterWallet;
    private readonly ICharacterProvider _characterProvider;
    private readonly CharacterStats _characterStats;
    private readonly IPanelService _panelService;
    private readonly DiContainer _container;
    private readonly LevelsConfiguration _levelsConfiguration;

    public GoldDropper(GoldDropperConfiguration configuration, CharacterWallet characterWallet,
        ICharacterProvider characterProvider, CharacterStats characterStats, IPanelService panelService,
        DiContainer container, LevelsConfiguration levelsConfiguration)
    {
        _configuration = configuration;
        _characterWallet = characterWallet;
        _characterProvider = characterProvider;
        _characterStats = characterStats;
        _panelService = panelService;
        _container = container;
        _levelsConfiguration = levelsConfiguration;
    }

    public void DropGold(Vector3 position)
    {
        float dropChance = Mathf.Clamp01(_configuration?.DropChance ?? 0.5f);
        if (Random.value >= dropChance)
            return;

        int amount = CalculateGoldReward(GetBaseGoldAmount());
        if (amount <= 0)
            return;

        if (_configuration == null || _configuration.CoinGoldPrefab == null)
        {
            CollectInstantly(amount);
            return;
        }

        Vector3 landPosition = GetGroundedPosition(position + GetScatterOffset());
        Vector3 spawnPosition = landPosition + Vector3.up * _configuration.DropHeight;
        GameObject coinObject = _container.InstantiatePrefab(_configuration.CoinGoldPrefab, spawnPosition,
            Quaternion.identity, null);

        CoinGold coinGold = coinObject.GetComponent<CoinGold>();
        if (coinGold == null)
            coinGold = coinObject.AddComponent<CoinGold>();

        coinGold.Construct(amount, _configuration, _characterWallet, _characterProvider, _characterStats,
            _panelService, landPosition);
    }

    private int GetBaseGoldAmount() =>
        Mathf.Max(1, _configuration?.BaseGoldAmount ?? 1);

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

    private int CalculateGoldReward(int baseReward)
    {
        float scaledReward = baseReward * (1f + Mathf.Max(0f, _characterStats.GainGold) * 0.01f);
        int reward = Mathf.FloorToInt(scaledReward);

        if (Random.value < scaledReward - reward)
            reward++;

        float luckChance = Mathf.Clamp(_characterStats.Luck, 0f, 100f) * 0.01f;
        if (Random.value < luckChance)
            reward += baseReward;

        return Mathf.Max(1, reward);
    }

    private void CollectInstantly(int amount)
    {
        _characterWallet.Gold.Add(amount);

        if (_panelService?.GetPanel(PanelName.CharacterPanel) is CharacterPanel characterPanel)
            characterPanel.CharacterGoldView.ShowGold(amount);
    }
}
