using System;
using System.Linq;
using Features.Enemies.Scripts;
using UnityEngine;
using Zenject;

namespace Features.RewardBag
{
    public sealed class RewardBagSpawner
    {
        private const float SpawnDistanceFromCharacter = 5f;
        private const float GroundRayStartHeight = 20f;
        private const float GroundRayDistance = 50f;

        private readonly GameObject _rewardBagPrefab;
        private readonly ICharacterProvider _characterProvider;
        private readonly CharacterWallet _characterWallet;
        private readonly LevelsConfiguration _levelsConfiguration;
        private readonly DiContainer _container;

        public event Action<DefaultEnemiesRoomData> RewardCollected;

        public RewardBagSpawner(GameObject rewardBagPrefab, ICharacterProvider characterProvider,
            CharacterWallet characterWallet, LevelsConfiguration levelsConfiguration,
            DiContainer container)
        {
            _rewardBagPrefab = rewardBagPrefab;
            _characterProvider = characterProvider;
            _characterWallet = characterWallet;
            _levelsConfiguration = levelsConfiguration;
            _container = container;
        }

        public bool TrySpawn(DefaultEnemiesRoomData roomData, LevelView level)
        {
            if (_rewardBagPrefab == null || roomData == null || level == null ||
                _characterProvider?.CharacterFacade == null)
                return false;

            Room room = level.Rooms
                .FirstOrDefault(node => node != null && node.Room != null &&
                                        ReferenceEquals(node.Room.RoomData, roomData))
                ?.Room;
            if (room == null)
                return false;

            Vector3 groundPoint = GetGroundPoint(room);
            GameObject bagObject = _container.InstantiatePrefab(_rewardBagPrefab,
                groundPoint + Vector3.up * 0.5f, Quaternion.identity, room.transform);
            RewardBag rewardBag = bagObject.GetComponent<RewardBag>();
            if (rewardBag == null)
            {
                UnityEngine.Object.Destroy(bagObject);
                return false;
            }

            AlignBottomToGround(bagObject, groundPoint.y);
            rewardBag.Construct(_characterProvider, _characterWallet,
                () => RewardCollected?.Invoke(roomData));
            return true;
        }

        private Vector3 GetGroundPoint(Room room)
        {
            Transform character = _characterProvider.CharacterFacade.transform;
            Vector3 directionToRoomCenter = room.transform.position - character.position;
            directionToRoomCenter.y = 0f;

            if (directionToRoomCenter.sqrMagnitude <= 0.001f)
            {
                directionToRoomCenter = character.forward;
                directionToRoomCenter.y = 0f;
            }

            Vector3 desiredPosition = character.position +
                                      directionToRoomCenter.normalized * SpawnDistanceFromCharacter;
            Vector3 rayOrigin = desiredPosition + Vector3.up * GroundRayStartHeight;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, GroundRayDistance,
                    GetGroundLayerMask(), QueryTriggerInteraction.Ignore))
                return hit.point;

            return new Vector3(desiredPosition.x, character.position.y, desiredPosition.z);
        }

        private LayerMask GetGroundLayerMask() =>
            _levelsConfiguration != null && _levelsConfiguration.GroundLayer.value != 0
                ? _levelsConfiguration.GroundLayer
                : Physics.DefaultRaycastLayers;

        private static void AlignBottomToGround(GameObject bagObject, float groundY)
        {
            Renderer[] renderers = bagObject.GetComponentsInChildren<Renderer>()
                .Where(renderer => renderer is not ParticleSystemRenderer)
                .ToArray();
            if (renderers.Length == 0)
                return;

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);

            bagObject.transform.position += Vector3.up * (groundY - bounds.min.y);
        }
    }
}
