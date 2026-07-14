using UnityEngine;
using Zenject;

namespace Features.Relics.Scripts
{
    internal sealed class RelicChestRewardPresenter
    {
        private const float RewardScale = 0.5f;
        private const int RewardSortingOrder = 10;

        private readonly RelicChestConfiguration _configuration;
        private readonly RelicManager _relicManager;
        private readonly RelicEventBus _eventBus;
        private readonly DiContainer _container;
        private readonly RoomData _roomData;
        private readonly Room _room;

        public RelicChestRewardPresenter(RelicChestConfiguration configuration,
            RelicManager relicManager, RelicEventBus eventBus, DiContainer container,
            RoomData roomData, Room room)
        {
            _configuration = configuration;
            _relicManager = relicManager;
            _eventBus = eventBus;
            _container = container;
            _roomData = roomData;
            _room = room;
        }

        public bool TryPresent(RelicDefinition relic, Transform spawnRoot, out GameObject rewardObject)
        {
            RelicPickup pickup = _container.InstantiatePrefabForComponent<RelicPickup>(
                _configuration.RelicPickupPrefab,
                spawnRoot.position,
                Quaternion.identity,
                spawnRoot);

            rewardObject = pickup.gameObject;
            pickup.enabled = false;
            pickup.name = $"RelicReward_{relic.Id}";
            pickup.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            pickup.transform.localScale = Vector3.one * RewardScale;

            SpriteRenderer spriteRenderer = pickup.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = relic.Icon;
                spriteRenderer.sortingOrder = RewardSortingOrder;
            }

            if (_relicManager.AddRelic(relic) == false)
            {
                Debug.LogError($"Failed to grant relic {relic.Id} from chest.", pickup);
                return false;
            }

            _eventBus.PublishChestCollected(_roomData, _room);
            return true;
        }
    }
}
