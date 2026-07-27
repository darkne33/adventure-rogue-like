using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Zenject;

namespace Features.Relics.Scripts
{
    internal sealed class RelicChestRewardPresenter
    {
        private readonly RelicChestConfiguration _configuration;
        private readonly RelicManager _relicManager;
        private readonly RelicEventBus _eventBus;
        private readonly ICharacterProvider _characterProvider;
        private readonly DiContainer _container;
        private readonly RoomData _roomData;
        private readonly Room _room;

        public RelicChestRewardPresenter(RelicChestConfiguration configuration,
            RelicManager relicManager, RelicEventBus eventBus,
            ICharacterProvider characterProvider, DiContainer container, RoomData roomData,
            Room room)
        {
            _configuration = configuration;
            _relicManager = relicManager;
            _eventBus = eventBus;
            _characterProvider = characterProvider;
            _container = container;
            _roomData = roomData;
            _room = room;
        }

        public bool TryCreatePreview(RelicDefinition relic, Transform spawnRoot,
            out RelicPickup preview)
        {
            preview = null;
            if (relic == null || spawnRoot == null || _configuration.RelicPickupPrefab == null)
                return false;

            RelicPickup pickup = _container.InstantiatePrefabForComponent<RelicPickup>(
                _configuration.RelicPickupPrefab,
                spawnRoot.position,
                Quaternion.identity,
                spawnRoot);

            preview = pickup;
            pickup.name = "RelicChestPreview";
            pickup.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            UpdatePreview(pickup, relic);
            return true;
        }

        public void UpdatePreview(RelicPickup preview, RelicDefinition relic)
        {
            if (preview == null || relic == null)
                return;

            preview.transform.DOKill();
            preview.transform.localScale = Vector3.one * _configuration.RelicPreviewScale;
            preview.SetVisual(relic);
        }

        public async UniTask<bool> GrantAndDismissAsync(RelicPickup preview,
            RelicDefinition relic)
        {
            bool granted = preview != null &&
                           await preview.CollectImmediatelyAsync(relic, _configuration,
                               _relicManager, _eventBus, _characterProvider, _roomData, _room);
            if (granted == false)
                Debug.LogError($"Failed to grant relic {relic?.Id} from chest.", preview);

            return granted;
        }
    }
}
