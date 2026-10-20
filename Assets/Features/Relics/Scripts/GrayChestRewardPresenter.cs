using System;
using Features.RewardBag;
using UnityEngine;
using Zenject;

namespace Features.Relics.Scripts
{
    internal sealed class GrayChestRewardPresenter
    {
        private enum RewardType
        {
            Coins,
            Key,
            Heart,
            CoinsAndKey,
            CoinsAndHeart,
            HeartAndKey
        }

        private readonly RelicChestConfiguration _configuration;
        private readonly DiContainer _container;
        private readonly ICharacterProvider _characterProvider;
        private readonly CharacterWallet _wallet;
        private readonly CharacterStats _stats;
        private readonly HeartDropper _heartDropper;
        private readonly LevelsConfiguration _levelsConfiguration;
        private readonly RelicEventBus _eventBus;
        private readonly Room _room;

        public bool IsConfigured => _configuration != null && _container != null &&
                                    _characterProvider != null && _wallet != null &&
                                    _heartDropper != null && GetTotalWeight() > 0f &&
                                    HasPickup(_configuration.GrayChestGoldRewardPrefab) &&
                                    HasPickup(_configuration.GrayChestKeyRewardPrefab);

        public GrayChestRewardPresenter(RelicChestConfiguration configuration,
            DiContainer container, ICharacterProvider characterProvider,
            CharacterWallet wallet, CharacterStats stats, HeartDropper heartDropper,
            LevelsConfiguration levelsConfiguration, RelicEventBus eventBus, Room room)
        {
            _configuration = configuration;
            _container = container;
            _characterProvider = characterProvider;
            _wallet = wallet;
            _stats = stats;
            _heartDropper = heartDropper;
            _levelsConfiguration = levelsConfiguration;
            _eventBus = eventBus;
            _room = room;
        }

        public void DropRewards(Vector3 chestPosition, Vector3 spawnPosition)
        {
            RewardType reward = RollReward();
            if (reward is RewardType.Coins or RewardType.CoinsAndKey or RewardType.CoinsAndHeart)
            {
                int minimum = Mathf.Max(1, _configuration.GrayChestMinCoins);
                int maximum = Mathf.Max(minimum, _configuration.GrayChestMaxCoins);
                int coinCount = UnityEngine.Random.Range(minimum, maximum + 1);
                for (int index = 0; index < coinCount; index++)
                {
                    DropCurrency(_configuration.GrayChestGoldRewardPrefab,
                        chestPosition, spawnPosition, () => _wallet.Gold.Add(1));
                }
            }

            if (reward is RewardType.Key or RewardType.CoinsAndKey or RewardType.HeartAndKey)
            {
                DropCurrency(_configuration.GrayChestKeyRewardPrefab,
                    chestPosition, spawnPosition, () => _wallet.Keys.Add(1));
            }

            if (reward is RewardType.Heart or RewardType.CoinsAndHeart or RewardType.HeartAndKey)
            {
                float additionalHeight = Mathf.Max(0f, spawnPosition.y - chestPosition.y);
                if (_heartDropper.DropHeart(chestPosition,
                        additionalDropHeight: additionalHeight) == false)
                    Debug.LogError("Gray chest could not spawn its heart reward.");
            }

            if (_room != null)
                _eventBus.PublishChestCollected(_room.RoomData, _room);
        }

        private RewardType RollReward()
        {
            float roll = UnityEngine.Random.value * GetTotalWeight();
            float threshold = Mathf.Max(0f, _configuration.GrayChestCoinsWeight);
            if (roll < threshold)
                return RewardType.Coins;

            threshold += Mathf.Max(0f, _configuration.GrayChestKeyWeight);
            if (roll < threshold)
                return RewardType.Key;

            threshold += Mathf.Max(0f, _configuration.GrayChestHeartWeight);
            if (roll < threshold)
                return RewardType.Heart;

            threshold += Mathf.Max(0f, _configuration.GrayChestCoinsAndKeyWeight);
            if (roll < threshold)
                return RewardType.CoinsAndKey;

            threshold += Mathf.Max(0f, _configuration.GrayChestCoinsAndHeartWeight);
            return roll < threshold ? RewardType.CoinsAndHeart : RewardType.HeartAndKey;
        }

        private float GetTotalWeight() =>
            Mathf.Max(0f, _configuration.GrayChestCoinsWeight) +
            Mathf.Max(0f, _configuration.GrayChestKeyWeight) +
            Mathf.Max(0f, _configuration.GrayChestHeartWeight) +
            Mathf.Max(0f, _configuration.GrayChestCoinsAndKeyWeight) +
            Mathf.Max(0f, _configuration.GrayChestCoinsAndHeartWeight) +
            Mathf.Max(0f, _configuration.GrayChestHeartAndKeyWeight);

        private void DropCurrency(GameObject prefab, Vector3 chestPosition,
            Vector3 spawnPosition, Action grantReward)
        {
            RewardBagPickup pickup = _container.InstantiatePrefabForComponent<RewardBagPickup>(
                prefab, spawnPosition, Quaternion.identity,
                _room != null ? _room.transform : null);
            pickup.transform.localScale = Vector3.one *
                                          Mathf.Max(0.01f, _configuration.GrayChestRewardScale);
            pickup.Construct(_characterProvider, _stats,
                GetLandPosition(chestPosition), grantReward, null);
        }

        private Vector3 GetLandPosition(Vector3 chestPosition)
        {
            const float groundOffset = 0.35f;
            Vector2 scatter = UnityEngine.Random.insideUnitCircle *
                              Mathf.Max(0f, _configuration.GrayChestRewardScatterRadius);
            Vector3 candidate = chestPosition + new Vector3(scatter.x, 0f, scatter.y);
            LayerMask groundLayer = _levelsConfiguration != null &&
                                    _levelsConfiguration.GroundLayer.value != 0
                ? _levelsConfiguration.GroundLayer
                : Physics.DefaultRaycastLayers;
            if (Physics.Raycast(candidate + Vector3.up * 4f, Vector3.down,
                    out RaycastHit hit, 12f, groundLayer, QueryTriggerInteraction.Ignore))
                return hit.point + Vector3.up * groundOffset;

            return candidate + Vector3.up * groundOffset;
        }

        private static bool HasPickup(GameObject prefab) =>
            prefab != null && prefab.GetComponent<RewardBagPickup>() != null;
    }
}
