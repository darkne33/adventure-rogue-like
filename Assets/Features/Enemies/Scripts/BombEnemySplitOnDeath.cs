using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;
using Zenject;

namespace Features.Enemies.Scripts
{
    [DisallowMultipleComponent]
    public sealed class BombEnemySplitOnDeath : MonoBehaviour
    {
        [SerializeField] private GameObject _normalBombPrefab;
        [Min(1)]
        [SerializeField] private int _spawnCount = 2;
        [Min(0f)]
        [SerializeField] private float _landingDistance = 2.6f;
        [Min(0.1f)]
        [SerializeField] private float _landingSearchDistance = 2f;
        [Min(0f)]
        [SerializeField] private float _spawnHeight = 2f;
        [Min(0f)]
        [SerializeField] private float _jumpPower = 1f;
        [Min(0.01f)]
        [SerializeField] private float _dropDuration = 0.65f;

        private IEnemyFactory _enemyFactory;
        private IEnemiesProvider _enemiesProvider;
        private bool _hasSpawned;

        [Inject]
        private void Construct(IEnemyFactory enemyFactory, IEnemiesProvider enemiesProvider)
        {
            _enemyFactory = enemyFactory;
            _enemiesProvider = enemiesProvider;
        }

        public void SpawnNormalBombs()
        {
            if (_hasSpawned || _normalBombPrefab == null)
                return;

            _hasSpawned = true;

            Vector3 origin = transform.position;
            float angleStep = 360f / _spawnCount;
            float startAngle = UnityEngine.Random.Range(0f, 360f);

            for (int i = 0; i < _spawnCount; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, startAngle + angleStep * i, 0f) *
                                    Vector3.forward;
                Vector3 landingPosition = FindLandingPosition(origin, direction);
                Vector3 spawnPosition = origin + Vector3.up * _spawnHeight;
                EnemyFacade spawnedBomb = _enemyFactory.Create(
                    _normalBombPrefab, spawnPosition, landingPosition);

                _enemiesProvider.AddEnemy(spawnedBomb);
                spawnedBomb.SetStop(true);
                spawnedBomb.AnimationSystem?.IdleAnimation();
                DropBomb(spawnedBomb, landingPosition).Forget();
            }
        }

        private Vector3 FindLandingPosition(Vector3 origin, Vector3 direction)
        {
            Vector3 desiredPosition = origin + direction * _landingDistance;
            if (NavMesh.SamplePosition(desiredPosition, out NavMeshHit landingHit,
                    _landingSearchDistance, NavMesh.AllAreas))
            {
                return landingHit.position;
            }

            if (NavMesh.SamplePosition(origin, out NavMeshHit originHit,
                    _landingSearchDistance, NavMesh.AllAreas))
            {
                return originHit.position;
            }

            return origin;
        }

        private async UniTaskVoid DropBomb(EnemyFacade spawnedBomb, Vector3 landingPosition)
        {
            try
            {
                await spawnedBomb.transform
                    .DOJump(landingPosition, _jumpPower, 1, _dropDuration)
                    .SetEase(Ease.OutQuad)
                    .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait,
                        spawnedBomb.GetCancellationTokenOnDestroy());

                if (spawnedBomb == null)
                    return;

                spawnedBomb.SyncNavigationPosition();
                spawnedBomb.SetStop(false);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
