using Core.Services;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Features.Enemies.Scripts;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner
{
    private readonly IRogueLikeRuntimeDataService _rogueLikeRuntimeDataService;
    private readonly IEnemyFactory _enemyFactory;
    private readonly LevelsConfiguration _levelsConfiguration;
    private readonly IEnemiesProvider _enemiesProvider;
    private readonly IEffectsService _effectsService;

    private readonly Vector2 _spawnRadius = new Vector2(0, 40f);

    private const float RayStartHeight = 50f;
    private const float RayDistance = 100f;
    private const float NavMeshSampleDistance = 1.5f;
    private const float MaxGroundNavMeshHeightDifference = 0.5f;
    private const float ObstacleCheckRadius = 1f;
    private const float ObstacleCheckHeight = 1f;

    public EnemySpawner(IRogueLikeRuntimeDataService rogueLikeRuntimeDataService, IEnemyFactory enemyFactory,
        LevelsConfiguration levelsConfiguration, IEnemiesProvider enemiesProvider, IEffectsService effectsService)
    {
        _rogueLikeRuntimeDataService = rogueLikeRuntimeDataService;
        _enemyFactory = enemyFactory;
        _levelsConfiguration = levelsConfiguration;
        _enemiesProvider = enemiesProvider;
        _effectsService = effectsService;
    }

    public void TrySpawnEnemies(CharacterFacade characterFacade, int currentWave)
    {
        if (characterFacade == null)
            throw new System.ArgumentNullException(nameof(characterFacade));

        if (_rogueLikeRuntimeDataService.CurrentRoomData is not DefaultEnemiesRoomData currentRoomData)
            throw new System.InvalidOperationException("Enemies can only be spawned in a default enemies room.");

        if (currentRoomData.EnemyWavesConfiguration == null ||
            currentWave < 0 ||
            currentWave >= currentRoomData.EnemyWavesConfiguration.Length)
            throw new System.ArgumentOutOfRangeException(nameof(currentWave), currentWave,
                "Wave index is outside the current room wave configuration.");

        EnemyWavesConfiguration wave = currentRoomData.EnemyWavesConfiguration[currentWave];
        if (wave == null || wave.EnemyTypes == null)
            throw new System.InvalidOperationException($"Wave {currentWave} is not configured correctly.");

        LevelSettings levelSettings =
            _levelsConfiguration.GetLevel(_rogueLikeRuntimeDataService.CurrentIndexLevel);

        if (levelSettings.EnemyFactoryConfiguration == null)
            throw new System.InvalidOperationException(
                "Enemy factory configuration is missing for the current level.");

        for (int i = 0; i < wave.EnemyTypes.Length; i++)
        {
            var enemyType = wave.EnemyTypes[i];

            if (!TryFindValidSpawnPosition(characterFacade.transform.position, out var spawnPosition))
            {
                Debug.LogWarning($"Could not find valid spawn position for enemy {enemyType} after max attempts.");
                continue;
            }

            var enemy = levelSettings.EnemyFactoryConfiguration.GetEnemyByType(enemyType);

            SpawnEnemy(enemy, spawnPosition).Forget();
        }
    }

    private async UniTask SpawnEnemy(GameObject enemy, Vector3 spawnPosition)
    {
        var offsetDown = 2f;
        Vector3 underGroundPosition = spawnPosition + Vector3.down * offsetDown;
        EnemyFacade enemyFacade = _enemyFactory.Create(enemy, underGroundPosition, spawnPosition);
        _enemiesProvider.AddEnemy(enemyFacade);

        var portalEffect = _effectsService.GetEffect(EffectName.EnemyPortal);
        var defaultScaleEffect = portalEffect.transform.localScale;
        portalEffect.transform.position = spawnPosition + Vector3.up * 0.1f;
        portalEffect.PlayWithoutRelease();

        try
        {
            enemyFacade.SetStop(true);

            await enemyFacade.transform.DOMoveY(spawnPosition.y, 0.5f)
                .ToUniTask(cancellationToken: enemyFacade.GetCancellationTokenOnDestroy());

            if (enemyFacade != null)
                enemyFacade.SetStop(false);

            await portalEffect.transform.DOScale(Vector3.zero, 0.3f).ToUniTask();
        }
        catch (System.OperationCanceledException)
        {
        }
        finally
        {
            portalEffect.transform.DOKill();
            portalEffect.transform.localScale = defaultScaleEffect;
            portalEffect.Release();
        }
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
        finalPosition = Vector3.zero;

        Vector3 rayOrigin = position + Vector3.up * RayStartHeight;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, RayDistance,
                _levelsConfiguration.GroundLayer, QueryTriggerInteraction.Ignore) == false)
        {
            return false;
        }

        if (NavMesh.SamplePosition(hit.point, out NavMeshHit navMeshHit, NavMeshSampleDistance,
                NavMesh.AllAreas) == false)
        {
            return false;
        }

        if (Mathf.Abs(navMeshHit.position.y - hit.point.y) > MaxGroundNavMeshHeightDifference)
        {
            return false;
        }

        finalPosition = navMeshHit.position;
        Vector3 obstacleCheckPosition = finalPosition + Vector3.up * ObstacleCheckHeight;
        Collider[] colliders = Physics.OverlapSphere(obstacleCheckPosition, ObstacleCheckRadius,
            _levelsConfiguration.ObstacleLayer, QueryTriggerInteraction.Ignore);

        return colliders.Length == 0;
    }
}
