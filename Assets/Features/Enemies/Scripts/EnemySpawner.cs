using Features.Enemies.Scripts;
using UnityEngine;

public class EnemySpawner
{
    private readonly IRogueLikeRuntimeDataService _rogueLikeRuntimeDataService;
    private readonly IEnemyFactory _enemyFactory;
    private readonly LevelsConfiguration _levelsConfiguration;
    private readonly IEnemiesProvider _enemiesProvider;

    private int _currentWave = 0;

    private Vector2 _spawnRadius = new Vector2(5f, 10f);

    private LayerMask _groundLayer;
    private LayerMask _obstacleLayer;
    private float _maxHeightDifference = 1f;


    public EnemySpawner(IRogueLikeRuntimeDataService rogueLikeRuntimeDataService, IEnemyFactory enemyFactory,
        LevelsConfiguration levelsConfiguration, IEnemiesProvider enemiesProvider)
    {
        _rogueLikeRuntimeDataService = rogueLikeRuntimeDataService;
        _enemyFactory = enemyFactory;
        _levelsConfiguration = levelsConfiguration;
        _enemiesProvider = enemiesProvider;
    }

    public void TrySpawnEnemies(CharacterFacade characterFacade)
    {
        var wave = _levelsConfiguration.Levels[_rogueLikeRuntimeDataService.CurrentIndexLevel]
            .EnemyWavesConfiguration[_currentWave];
        
        Debug.Log(wave.EnemyTypes.Length);

        for (int attempt = 0; attempt < wave.EnemyTypes.Length; attempt++)
        {
            Vector3 spawnPosition =
                GetRandomPointInAnnulus(characterFacade.transform.position, _spawnRadius.x, _spawnRadius.y);
            
            Debug.Log(spawnPosition);

            if (IsPositionValid(spawnPosition, out Vector3 finalPosition))
            {
                var enemyType = wave.EnemyTypes[attempt];
                var enemy = _levelsConfiguration.Levels[_rogueLikeRuntimeDataService.CurrentIndexLevel]
                    .EnemyFactoryConfiguration.GetEnemyByType(enemyType);
                EnemyFacade enemyFacade = _enemyFactory.Create(enemy, null);
                enemyFacade.transform.position = finalPosition;
                _enemiesProvider.AddEnemy(enemyFacade);
            }
        }
    }

    private Vector3 GetRandomPointInAnnulus(Vector3 center, float minRadius, float maxRadius)
    {
        float angle = Random.Range(0f, 360f);
        float distance = Random.Range(minRadius, maxRadius);
        Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        return center + direction * distance;
    }

    private bool IsPositionValid(Vector3 position, out Vector3 finalPosition)
    {
        finalPosition = position;

        if (!Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f,
                _levelsConfiguration.GroundLayer))
        {
            
            Debug.Log("TEST");
            return false;
        }

        if (Mathf.Abs(hit.point.y - position.y) > _maxHeightDifference)
        {
            Debug.Log("TEST");
            return false;
        }

        finalPosition = hit.point;

        var colliders = Physics.OverlapSphere(finalPosition, 0.5f, _levelsConfiguration.ObstacleLayer);
        if (colliders.Length > 0)
        {
            Debug.Log("TEST");
            return false;
        }

        /*if (!UnityEngine.AI.NavMesh.SamplePosition(finalPosition, out _, 1f, UnityEngine.AI.NavMesh.AllAreas))
        {
            return false;
        }*/

        return true;
    }
}