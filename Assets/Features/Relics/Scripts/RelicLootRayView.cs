using UnityEngine;

namespace Features.Relics.Scripts
{
    [DefaultExecutionOrder(100)]
    public sealed class RelicLootRayView : MonoBehaviour
    {
        [SerializeField] private RelicPickup _pickup;
        [SerializeField] private Transform _lootRayRoot;

        private Vector3 _lootRayPosition;
        private bool _hasLootRayPosition;

        private void OnEnable()
        {
            if (_pickup == null)
                return;

            _pickup.AvailabilityChanged += HandleAvailabilityChanged;
            HandleAvailabilityChanged(_pickup.IsAvailable);
        }

        private void OnDisable()
        {
            if (_pickup != null)
                _pickup.AvailabilityChanged -= HandleAvailabilityChanged;

            if (_lootRayRoot != null)
                _lootRayRoot.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_lootRayRoot != null && _hasLootRayPosition && _lootRayRoot.gameObject.activeSelf)
                _lootRayRoot.SetPositionAndRotation(_lootRayPosition, Quaternion.identity);
        }

        private void HandleAvailabilityChanged(bool isAvailable)
        {
            if (_lootRayRoot == null)
                return;

            if (isAvailable)
            {
                if (!_hasLootRayPosition)
                {
                    _lootRayPosition = _lootRayRoot.position;
                    if (_pickup.RoomData is OnlyRelicRoomData relicRoomData)
                        _lootRayPosition -= _pickup.transform.position - relicRoomData.RelicSpawnPoint.position;
                    _hasLootRayPosition = true;
                }

                _lootRayRoot.SetPositionAndRotation(_lootRayPosition, Quaternion.identity);
            }

            _lootRayRoot.gameObject.SetActive(isAvailable);
        }
    }
}
