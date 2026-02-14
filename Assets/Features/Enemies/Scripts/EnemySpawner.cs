using Core.Services;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Features.Enemies.Scripts;
using UnityEngine;

public class EnemySpawner
{
    private readonly IRogueLikeRuntimeDataService _rogueLikeRuntimeDataService;
    private readonly IEnemyFactory _enemyFactory;
    private readonly LevelsConfiguration _levelsConfiguration;
    private readonly IEnemiesProvider _enemiesProvider;
    private readonly IEffectsService _effectsService;

    private int _currentWave = 0;

    private readonly Vector2 _spawnRadius = new Vector2(0, 40f);

    private LayerMask _groundLayer;
    private LayerMask _obstacleLayer;

    public EnemySpawner(IRogueLikeRuntimeDataService rogueLikeRuntimeDataService, IEnemyFactory enemyFactory,
        LevelsConfiguration levelsConfiguration, IEnemiesProvider enemiesProvider, IEffectsService effectsService)
    {
        _rogueLikeRuntimeDataService = rogueLikeRuntimeDataService;
        _enemyFactory = enemyFactory;
        _levelsConfiguration = levelsConfiguration;
        _enemiesProvider = enemiesProvider;
        _effectsService = effectsService;
    }

    public void TrySpawnEnemies(CharacterFacade characterFacade)
    {
        var wave = _levelsConfiguration.Levels[_rogueLikeRuntimeDataService.CurrentIndexLevel]
            .EnemyWavesConfiguration[_currentWave];

        for (int i = 0; i < wave.EnemyTypes.Length; i++)
        {
            var enemyType = wave.EnemyTypes[i];

            if (!TryFindValidSpawnPosition(characterFacade.transform.position, out var spawnPosition))
            {
                Debug.LogWarning($"Could not find valid spawn position for enemy {enemyType} after max attempts.");
                continue;
            }

            var enemy = _levelsConfiguration.Levels[_rogueLikeRuntimeDataService.CurrentIndexLevel]
                .EnemyFactoryConfiguration.GetEnemyByType(enemyType);

            SpawnEnemy(enemy, spawnPosition).Forget();
        }
    }

    private async UniTask SpawnEnemy(GameObject enemy, Vector3 spawnPosition)
    {
        EnemyFacade enemyFacade = _enemyFactory.Create(enemy, null);

        var offsetDown = 2f;
        Vector3 underGroundPosition = spawnPosition + Vector3.down * offsetDown;
        enemyFacade.transform.position = underGroundPosition;

        var portalEffect = _effectsService.GetEffect(EffectName.EnemyPortal);
        portalEffect.transform.position = spawnPosition + Vector3.down * 0.9f;
        portalEffect.PlayWithoutRelease();

        enemyFacade.SetStop(true);

        await enemyFacade.transform.DOMoveY(spawnPosition.y, 0.5f)
            .ToUniTask(cancellationToken: enemyFacade.GetCancellationTokenOnDestroy());

        enemyFacade.SetStop(false);

        _enemiesProvider.AddEnemy(enemyFacade);
    }

    private Vector3 GetRandomPointInAnnulus(Vector3 center, float minRadius, float maxRadius)
    {
        float angle = Random.Range(0f, 360f);
        float distance = Random.Range(minRadius, maxRadius);
        Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        return center + direction * distance;
    }

    private bool TryFindValidSpawnPosition(Vector3 center, out Vector3 validPosition)
    {
        const int maxAttempts = 50;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector3 candidate = GetRandomPointInAnnulus(center, _spawnRadius.x, _spawnRadius.y);
            Debug.Log(candidate);
            if (IsPositionValid(candidate, out validPosition))
            {
                return true;
            }
        }

        validPosition = Vector3.zero;
        return false;
    }

    private bool IsPositionValid(Vector3 position, out Vector3 finalPosition)
    {
        finalPosition = position;

        float rayStartHeight = position.y + 10f;
        if (Physics.Raycast(new Vector3(position.x, rayStartHeight, position.z), Vector3.down, out RaycastHit hit, 100f,
                _levelsConfiguration.GroundLayer) == false)
        {
            return false;
        }

        finalPosition = new Vector3(hit.point.x, position.y, hit.point.z);

        var colliders = Physics.OverlapSphere(finalPosition, 1f, _levelsConfiguration.ObstacleLayer);
        if (colliders.Length > 0)
        {
            return false;
        }

        return true;
    }
}