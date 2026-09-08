using Core.Services;
using Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Features.Enemies.Scripts;
using Features.Relics.Scripts;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner
{
    private readonly IRogueLikeRuntimeDataService _rogueLikeRuntimeDataService;
    private readonly IEnemyFactory _enemyFactory;
    private readonly LevelsConfiguration _levelsConfiguration;
    private readonly IEnemiesProvider _enemiesProvider;
    private readonly IEffectsService _effectsService;
    private readonly RelicEventBus _relicEventBus;
    private readonly ISceneService<RogueLikeSceneProvider> _sceneService;
    private readonly EnemyRoomObserver _enemyRoomObserver;
    private readonly Dictionary<GameObject, EnemySpawnVolume> _spawnVolumes = new();
    private readonly Collider[] _spawnOverlapResults = new Collider[64];
    private DefaultEnemiesRoomData _activeRoomData;
    private CharacterFacade _activeCharacter;
    private int _spawnedEnemiesInCurrentRoom;
    private int _allEnemiesInCurrentRoom;
    private int _waveEnemyCount;
    private int _pendingEnemySpawnCount;
    private int _spawnGeneration;
    private EnemyRoomScalingConfiguration _roomBalance;
    private EnemyFactoryConfiguration _loadedEnemyFactoryConfiguration;
    private readonly List<EnemySpawnRule> _roomEnemyRules = new();
    private readonly Dictionary<EnemyType, int> _spawnedByType = new();
    private int _roomProgressIndex;
    private int _elitesSpawnedInCurrentRoom;
    private bool _isRoomSpawningActive;

    private const float RayStartHeight = 50f;
    private const float RayDistance = 100f;
    private const float NavMeshSampleDistance = 1.5f;
    private const float MaxGroundNavMeshHeightDifference = 0.5f;
    private const float GroundContactTolerance = 0.05f;
    private const float SpawnBoundsPadding = 0.05f;
    private const float FallbackSpawnRadius = 1f;
    private const float FallbackSpawnHeight = 2f;
    private const float EnemySpawnRiseDuration = 0.5f;
    private const float PortalFadeDuration = 0.3f;
    private const float SpawnExclusionRadius = 8f;

    public EnemySpawner(IRogueLikeRuntimeDataService rogueLikeRuntimeDataService, IEnemyFactory enemyFactory,
        LevelsConfiguration levelsConfiguration, IEnemiesProvider enemiesProvider, IEffectsService effectsService,
        RelicEventBus relicEventBus, ISceneService<RogueLikeSceneProvider> sceneService,
        EnemyRoomObserver enemyRoomObserver)
    {
        _rogueLikeRuntimeDataService = rogueLikeRuntimeDataService;
        _enemyFactory = enemyFactory;
        _levelsConfiguration = levelsConfiguration;
        _enemiesProvider = enemiesProvider;
        _effectsService = effectsService;
        _relicEventBus = relicEventBus;
        _sceneService = sceneService;
        _enemyRoomObserver = enemyRoomObserver;
        _enemiesProvider.EnemyRemoved += HandleEnemyRemoved;
    }

    public UniTask LoadEnemyPrefabs(CancellationToken cts) =>
        LoadEnemyPrefabs(_rogueLikeRuntimeDataService.CurrentIndexLevel, cts);

    public async UniTask LoadEnemyPrefabs(int levelIndex, CancellationToken cts)
    {
        LevelSettings levelSettings =
            _levelsConfiguration.GetLevel(levelIndex);
        if (levelSettings.EnemyFactoryConfiguration == null)
            throw new System.InvalidOperationException(
                "Enemy factory configuration is missing for the current level.");

        if (ReferenceEquals(_loadedEnemyFactoryConfiguration, levelSettings.EnemyFactoryConfiguration))
            return;

        foreach (var enemyPrefabData in levelSettings.EnemyFactoryConfiguration.EnemyPrefabs)
        {
            await enemyPrefabData.NormalPrefabContainer.Load(cts);

            if (enemyPrefabData.HasElitePrefab)
                await enemyPrefabData.ElitePrefabContainer.Load(cts);
        }

        _loadedEnemyFactoryConfiguration = levelSettings.EnemyFactoryConfiguration;
    }

    public void TrySpawnEnemies(CharacterFacade characterFacade)
    {
        EnemyRoomSettings configuration = GetCurrentConfiguration(characterFacade,
            out DefaultEnemiesRoomData currentRoomData, out LevelSettings levelSettings);

        LevelView currentLevel = _sceneService.GameSceneComponentsService?.CurrentLevel;
        if (currentLevel == null)
            throw new System.InvalidOperationException("Current level view is not available.");

        _roomBalance = _levelsConfiguration.EnemyRoomScalingConfiguration;
        if (_roomBalance == null)
            throw new System.InvalidOperationException("Enemy room balance configuration is missing.");

        _roomProgressIndex = _levelsConfiguration.GetCombatProgressIndex(
            _rogueLikeRuntimeDataService.CurrentIndexLevel, currentLevel, currentRoomData);
        _waveEnemyCount = _roomBalance.GetStartEnemyCount(_roomProgressIndex);
        _allEnemiesInCurrentRoom = _roomBalance.GetAllEnemyCount(_roomProgressIndex);
        _activeRoomData = currentRoomData;
        _activeCharacter = characterFacade;
        _spawnedEnemiesInCurrentRoom = 0;
        _pendingEnemySpawnCount = 0;
        _spawnGeneration++;
        _elitesSpawnedInCurrentRoom = 0;
        _spawnedByType.Clear();
        SelectRoomEnemyRules(configuration, levelSettings.EnemyFactoryConfiguration);
        _isRoomSpawningActive = true;

        int initialEnemyCount = Mathf.Min(_waveEnemyCount, _allEnemiesInCurrentRoom);
        List<EnemyType> enemyTypes = BuildSpawnQueue(initialEnemyCount);
        Room currentRoom = GetCurrentRoom(currentLevel, currentRoomData);
        SpawnEnemyTypes(currentRoom, levelSettings, enemyTypes, characterFacade, spawnImmediately: true);

        if (_pendingEnemySpawnCount == 0 &&
            (_spawnedEnemiesInCurrentRoom >= _allEnemiesInCurrentRoom ||
             _spawnedEnemiesInCurrentRoom == 0))
        {
            FinishCurrentRoomSpawning();
        }
    }

    public int TrySpawnAdditionalEnemies(CharacterFacade characterFacade, int enemyCount)
    {
        if (enemyCount <= 0 || _pendingEnemySpawnCount > 0)
            return 0;

        EnemyRoomSettings configuration = GetCurrentConfiguration(characterFacade,
            out DefaultEnemiesRoomData currentRoomData, out LevelSettings levelSettings);
        int remainingCapacity = _allEnemiesInCurrentRoom - _spawnedEnemiesInCurrentRoom;
        int clampedEnemyCount = Mathf.Min(enemyCount, Mathf.Max(0, remainingCapacity));
        if (clampedEnemyCount <= 0)
            return 0;

        List<EnemyType> enemyTypes = BuildSpawnQueue(clampedEnemyCount);

        LevelView currentLevel = _sceneService.GameSceneComponentsService?.CurrentLevel;
        if (currentLevel == null)
            throw new System.InvalidOperationException("Current level view is not available.");

        Room currentRoom = GetCurrentRoom(currentLevel, currentRoomData);
        return SpawnEnemyTypes(currentRoom, levelSettings, enemyTypes,
            characterFacade);
    }

    private void HandleEnemyRemoved(int activeEnemyCount)
    {
        if (_isRoomSpawningActive == false || _enemyRoomObserver.IsRoomCompleted)
            return;

        if (_pendingEnemySpawnCount > 0)
            return;

        if (_rogueLikeRuntimeDataService.CurrentRoomData is not DefaultEnemiesRoomData currentRoomData ||
            ReferenceEquals(currentRoomData, _activeRoomData) == false)
        {
            return;
        }

        int remainingEnemyCount = _allEnemiesInCurrentRoom - _spawnedEnemiesInCurrentRoom;
        if (remainingEnemyCount <= 0)
        {
            FinishCurrentRoomSpawning();
            return;
        }

        if (activeEnemyCount > _roomBalance.GetReinforcementThreshold(_roomProgressIndex))
            return;

        int spawnCount = Mathf.Min(_waveEnemyCount - activeEnemyCount, remainingEnemyCount);

        int spawnedEnemyCount = TrySpawnAdditionalEnemies(_activeCharacter, spawnCount);
        if (_pendingEnemySpawnCount == 0 &&
            (_spawnedEnemiesInCurrentRoom >= _allEnemiesInCurrentRoom ||
             (spawnedEnemyCount == 0 && activeEnemyCount <= 0)))
        {
            FinishCurrentRoomSpawning();
        }
    }

    private void FinishCurrentRoomSpawning()
    {
        if (_isRoomSpawningActive == false)
            return;

        _isRoomSpawningActive = false;
        _enemyRoomObserver.FinishEnemySpawning(_enemiesProvider.Count);
    }

    private EnemyRoomSettings GetCurrentConfiguration(CharacterFacade characterFacade,
        out DefaultEnemiesRoomData currentRoomData, out LevelSettings levelSettings)
    {
        if (characterFacade == null)
            throw new System.ArgumentNullException(nameof(characterFacade));

        if (_rogueLikeRuntimeDataService.CurrentRoomData is not DefaultEnemiesRoomData roomData)
            throw new System.InvalidOperationException("Enemies can only be spawned in a default enemies room.");

        EnemyRoomSettings configuration = roomData.EnemySettings;
        if (configuration == null || !configuration.HasSpawnableEnemies)
            throw new System.InvalidOperationException(
                "The current enemy room settings do not contain spawnable enemies.");

        levelSettings =
            _levelsConfiguration.GetLevel(_rogueLikeRuntimeDataService.CurrentIndexLevel);

        if (levelSettings.EnemyFactoryConfiguration == null)
            throw new System.InvalidOperationException(
                "Enemy factory configuration is missing for the current level.");

        currentRoomData = roomData;
        return configuration;
    }

    private int SpawnEnemyTypes(Room currentRoom, LevelSettings levelSettings,
        IReadOnlyList<EnemyType> enemyTypes, CharacterFacade characterFacade, bool spawnImmediately = false)
    {
        if (enemyTypes.Count == 0)
            return 0;

        // Record the whole wave before spawning, including when the opening wave finishes synchronously.
        _spawnedEnemiesInCurrentRoom += enemyTypes.Count;
        _pendingEnemySpawnCount += enemyTypes.Count;
        SpawnEnemyTypesInBatches(currentRoom, levelSettings, enemyTypes, characterFacade,
            _spawnGeneration, spawnImmediately).Forget();
        return enemyTypes.Count;
    }

    private async UniTask SpawnEnemyTypesInBatches(Room currentRoom, LevelSettings levelSettings,
        IReadOnlyList<EnemyType> enemyTypes, CharacterFacade characterFacade, int spawnGeneration,
        bool spawnImmediately)
    {
        if (spawnImmediately == false)
            await UniTask.NextFrame();
        if (IsSpawnRequestActive(currentRoom, characterFacade, spawnGeneration) == false)
            return;

        Physics.SyncTransforms();

        List<Collider> groundColliders = GetGroundColliders(currentRoom);
        if (groundColliders.Count == 0)
            Debug.LogWarning($"Could not find ground colliders in room {currentRoom.name}. " +
                             "Enemy spawning will keep retrying.");

        int batchSize = _roomBalance.SpawnBatchSize;
        var reusableSpawnPositions = new Dictionary<EnemySpawnVolume, Vector3>();
        var pendingEnemySpawns = new List<PendingEnemySpawn>();
        for (int i = 0; i < enemyTypes.Count; i++)
        {
            if (spawnImmediately == false && i > 0 && i % batchSize == 0)
                await UniTask.Delay(System.TimeSpan.FromSeconds(_roomBalance.SpawnBatchDelay));

            if (IsSpawnRequestActive(currentRoom, characterFacade, spawnGeneration) == false)
                return;

            var enemyType = enemyTypes[i];
            bool allowElite = _elitesSpawnedInCurrentRoom < _roomBalance.GetEliteLimit(_roomProgressIndex);
            GameObject enemy = levelSettings.EnemyFactoryConfiguration.GetEnemyByType(
                enemyType, _roomProgressIndex, allowElite, forceElite: _elitesSpawnedInCurrentRoom == 0);
            if (enemy.GetComponent<EnemyFacade>().Configuration.EnemyRank == EnemyRank.Elite ||
                enemy.GetComponent<BombEnemySplitOnDeath>() != null)
                _elitesSpawnedInCurrentRoom++;
            EnemySpawnVolume spawnVolume = GetSpawnVolume(enemy);

            Vector3 spawnPosition = default;
            bool hasSpawnPosition = groundColliders.Count > 0 &&
                                    (TryFindValidSpawnPosition(currentRoom, groundColliders,
                                         spawnVolume, characterFacade, out spawnPosition) ||
                                     reusableSpawnPositions.TryGetValue(spawnVolume,
                                         out spawnPosition));
            if (hasSpawnPosition == false)
            {
                pendingEnemySpawns.Add(new PendingEnemySpawn(enemy, spawnVolume, enemyType));
                continue;
            }

            reusableSpawnPositions[spawnVolume] = spawnPosition;
            SpawnEnemy(enemy, spawnPosition, enemyType).Forget();
            _pendingEnemySpawnCount--;
        }

        if (pendingEnemySpawns.Count > 0)
        {
            await RetryPendingEnemySpawns(currentRoom, groundColliders, pendingEnemySpawns,
                characterFacade, spawnGeneration, spawnImmediately);
        }

        if (IsSpawnRequestActive(currentRoom, characterFacade, spawnGeneration) &&
            _pendingEnemySpawnCount == 0)
        {
            // Deaths between batches may already have reached the reinforcement threshold.
            HandleEnemyRemoved(_enemiesProvider.Count);
        }
    }

    private async UniTask RetryPendingEnemySpawns(Room room,
        IReadOnlyList<Collider> groundColliders, List<PendingEnemySpawn> pendingEnemySpawns,
        CharacterFacade characterFacade, int spawnGeneration, bool spawnImmediately)
    {
        var reusableSpawnPositions = new Dictionary<EnemySpawnVolume, Vector3>();
        IReadOnlyList<Collider> availableGroundColliders = groundColliders;
        int pendingSpawnIndex = 0;

        while (pendingEnemySpawns.Count > 0 &&
               IsSpawnRequestActive(room, characterFacade, spawnGeneration))
        {
            if (spawnImmediately)
                await UniTask.NextFrame();
            else
                await UniTask.Delay(System.TimeSpan.FromSeconds(_roomBalance.SpawnBatchDelay));
            if (IsSpawnRequestActive(room, characterFacade, spawnGeneration) == false)
                return;

            if (availableGroundColliders.Count == 0)
                availableGroundColliders = GetGroundColliders(room);

            if (availableGroundColliders.Count > 0)
            {
                if (pendingSpawnIndex >= pendingEnemySpawns.Count)
                    pendingSpawnIndex = 0;

                PendingEnemySpawn pendingSpawn = pendingEnemySpawns[pendingSpawnIndex];
                bool hasSpawnPosition = reusableSpawnPositions.TryGetValue(
                                            pendingSpawn.SpawnVolume, out Vector3 spawnPosition) ||
                                        TryFindValidSpawnPosition(room, availableGroundColliders,
                                            pendingSpawn.SpawnVolume, characterFacade,
                                            out spawnPosition);
                if (hasSpawnPosition)
                {
                    reusableSpawnPositions[pendingSpawn.SpawnVolume] = spawnPosition;
                    SpawnEnemy(pendingSpawn.Enemy, spawnPosition, pendingSpawn.EnemyType).Forget();
                    pendingEnemySpawns.RemoveAt(pendingSpawnIndex);
                    _pendingEnemySpawnCount--;
                }
                else
                {
                    pendingSpawnIndex++;
                }
            }

        }
    }

    private bool IsSpawnRequestActive(Room room, CharacterFacade characterFacade,
        int spawnGeneration) =>
        _isRoomSpawningActive &&
        _spawnGeneration == spawnGeneration &&
        room != null &&
        characterFacade != null &&
        _enemyRoomObserver.IsRoomCompleted == false &&
        _rogueLikeRuntimeDataService.CurrentRoomData is DefaultEnemiesRoomData currentRoomData &&
        ReferenceEquals(currentRoomData, _activeRoomData) &&
        ReferenceEquals(room.RoomData, _activeRoomData);

    private void SelectRoomEnemyRules(EnemyRoomSettings configuration,
        EnemyFactoryConfiguration factory)
    {
        _roomEnemyRules.Clear();
        var specialRules = new List<EnemySpawnRule>();
        foreach (EnemySpawnRule rule in _roomBalance.EnemyRules)
        {
            if (rule == null || rule.Weight <= 0f || rule.FirstRoom > _roomProgressIndex + 1 ||
                System.Array.IndexOf(configuration.EnemyTypes, rule.EnemyType) < 0 ||
                !factory.EnemyPrefabs.Exists(prefab => prefab.EnemyType == rule.EnemyType))
                continue;

            if (rule.EnemyType == EnemyType.Dummy)
                _roomEnemyRules.Add(rule);
            else
                specialRules.Add(rule);
        }

        if (specialRules.Count > 0)
        {
            int first = _roomProgressIndex % specialRules.Count;
            for (int i = 0; i < specialRules.Count; i++)
            {
                if (specialRules[i].FirstRoom == _roomProgressIndex + 1)
                    first = i;
            }

            int count = Mathf.Min(specialRules.Count, _roomBalance.GetSpecialTypeLimit(_roomProgressIndex));
            for (int i = 0; i < count; i++)
                _roomEnemyRules.Add(specialRules[(first + i) % specialRules.Count]);
        }

        if (_roomEnemyRules.Count == 0)
            throw new System.InvalidOperationException("No enemies are unlocked for this combat room.");
    }

    private List<EnemyType> BuildSpawnQueue(int enemyCount)
    {
        var aliveByType = new Dictionary<EnemyType, int>();
        int rangedAlive = 0;
        foreach (EnemyFacade enemy in _enemiesProvider.ActiveEnemies)
        {
            if (enemy == null || enemy.IsDead)
                continue;
            aliveByType.TryGetValue(enemy.SpawnType, out int alive);
            aliveByType[enemy.SpawnType] = alive + 1;
            if (EnemyRoomScalingConfiguration.IsRanged(enemy.SpawnType))
                rangedAlive++;
        }

        float totalWeight = 0f;
        foreach (EnemySpawnRule rule in _roomEnemyRules)
            totalWeight += _roomBalance.GetWeight(rule, _roomProgressIndex);

        var spawnQueue = new List<EnemyType>(enemyCount);
        for (int i = 0; i < enemyCount; i++)
        {
            EnemySpawnRule selected = null;
            float bestScore = float.NegativeInfinity;
            foreach (EnemySpawnRule rule in _roomEnemyRules)
            {
                aliveByType.TryGetValue(rule.EnemyType, out int alive);
                if (rule.MaxAlive > 0 && alive >= rule.MaxAlive ||
                    EnemyRoomScalingConfiguration.IsRanged(rule.EnemyType) &&
                    rangedAlive >= _roomBalance.GetRangedLimit(_roomProgressIndex))
                    continue;

                _spawnedByType.TryGetValue(rule.EnemyType, out int spawned);
                float score = _roomBalance.GetWeight(rule, _roomProgressIndex) / totalWeight *
                              (_spawnedEnemiesInCurrentRoom + i + 1) - spawned;
                // Introduce each selected behaviour once in the opening group.
                if (spawned == 0)
                    score += _waveEnemyCount;
                if (score <= bestScore)
                    continue;
                selected = rule;
                bestScore = score;
            }

            if (selected == null)
                break;

            EnemyType type = selected.EnemyType;
            spawnQueue.Add(type);
            aliveByType.TryGetValue(type, out int aliveCount);
            aliveByType[type] = aliveCount + 1;
            _spawnedByType.TryGetValue(type, out int spawnedCount);
            _spawnedByType[type] = spawnedCount + 1;
            if (EnemyRoomScalingConfiguration.IsRanged(type))
                rangedAlive++;
        }

        return spawnQueue;
    }

    private async UniTask SpawnEnemy(GameObject enemy, Vector3 spawnPosition, EnemyType enemyType)
    {
        var offsetDown = 2f;
        Vector3 underGroundPosition = spawnPosition + Vector3.down * offsetDown;
        EnemyFacade enemyFacade = _enemyFactory.Create(enemy, underGroundPosition, spawnPosition);
        enemyFacade.SpawnType = enemyType;
        _enemiesProvider.AddEnemy(enemyFacade);

        if (enemyFacade.Configuration?.EnemyRank == EnemyRank.Boss)
            _relicEventBus.PublishBossSpawned(new RelicBossSpawnEvent(enemyFacade, spawnPosition));

        CancellationToken lifetimeToken = enemyFacade.GetCancellationTokenOnDestroy();
        UniTask portalLifetime = PlaySpawnPortal(spawnPosition, lifetimeToken);

        try
        {
            enemyFacade.SetStop(true);

            await enemyFacade.transform.DOMoveY(spawnPosition.y, EnemySpawnRiseDuration)
                .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, lifetimeToken);

            if (enemyFacade != null)
                enemyFacade.SetStop(false);
        }
        catch (System.OperationCanceledException)
        {
        }
        finally
        {
            await portalLifetime;
        }
    }

    private async UniTask PlaySpawnPortal(Vector3 spawnPosition, CancellationToken lifetimeToken)
    {
        EffectPlayer portalEffect = null;
        Vector3 defaultScale = Vector3.one;

        try
        {
            portalEffect = _effectsService.GetEffect(EffectName.EnemyPortal);
            if (portalEffect == null)
                return;

            portalEffect.transform.DOKill();
            defaultScale = portalEffect.transform.localScale;
            portalEffect.transform.position = spawnPosition + Vector3.up * 0.1f;
            portalEffect.PlayWithoutRelease();

            await portalEffect.transform.DOScale(Vector3.zero, PortalFadeDuration)
                .SetDelay(EnemySpawnRiseDuration)
                .SetUpdate(true)
                .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, lifetimeToken);
        }
        catch (System.OperationCanceledException)
        {
        }
        finally
        {
            if (portalEffect != null)
            {
                // ToUniTask resumes from OnKill, before DOTween finishes removing the tween.
                // KillAndCancelAwait already handles cancellation: do not kill it again here.
                portalEffect.transform.localScale = defaultScale;
                portalEffect.Release();
            }
        }
    }

    private static Room GetCurrentRoom(LevelView currentLevel, RoomData currentRoomData)
    {
        for (int i = 0; i < currentLevel.Rooms.Count; i++)
        {
            Room room = currentLevel.Rooms[i]?.Room;
            if (room != null && ReferenceEquals(room.RoomData, currentRoomData))
                return room;
        }

        throw new System.InvalidOperationException(
            $"{currentLevel.name} does not contain the current room data.");
    }

    private List<Collider> GetGroundColliders(Room room)
    {
        var groundColliders = new List<Collider>();
        Collider[] colliders = room.GetComponentsInChildren<Collider>(false);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || collider.enabled == false || collider.isTrigger ||
                ContainsLayer(_levelsConfiguration.GroundLayer, collider.gameObject.layer) == false)
                continue;

            groundColliders.Add(collider);
        }

        return groundColliders;
    }

    private bool TryFindValidSpawnPosition(Room room, IReadOnlyList<Collider> groundColliders,
        EnemySpawnVolume spawnVolume, CharacterFacade characterFacade,
        out Vector3 validPosition)
    {
        const int maxAttempts = 50;
        Vector3 characterPosition = characterFacade.transform.position;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector3 candidate = GetRandomSpawnCandidate(groundColliders, spawnVolume.Bounds);
            if (IsPositionValid(room, candidate, spawnVolume, out validPosition))
            {
                if (GetFlatSqrDistance(validPosition, characterPosition) <
                    SpawnExclusionRadius * SpawnExclusionRadius)
                {
                    continue;
                }

                return true;
            }
        }

        validPosition = Vector3.zero;
        return false;
    }

    private static float GetFlatSqrDistance(Vector3 first, Vector3 second)
    {
        float deltaX = first.x - second.x;
        float deltaZ = first.z - second.z;
        return deltaX * deltaX + deltaZ * deltaZ;
    }

    private static Vector3 GetRandomSpawnCandidate(IReadOnlyList<Collider> groundColliders,
        Bounds spawnBounds)
    {
        float totalArea = 0f;
        for (int i = 0; i < groundColliders.Count; i++)
            totalArea += GetHorizontalArea(groundColliders[i]);

        float areaRoll = Random.Range(0f, totalArea);
        Collider selectedCollider = groundColliders[groundColliders.Count - 1];
        for (int i = 0; i < groundColliders.Count; i++)
        {
            Collider groundCollider = groundColliders[i];
            areaRoll -= GetHorizontalArea(groundCollider);
            if (areaRoll > 0f)
                continue;

            selectedCollider = groundCollider;
            break;
        }

        Bounds bounds = selectedCollider.bounds;
        float minX = bounds.min.x - spawnBounds.min.x + SpawnBoundsPadding;
        float maxX = bounds.max.x - spawnBounds.max.x - SpawnBoundsPadding;
        float minZ = bounds.min.z - spawnBounds.min.z + SpawnBoundsPadding;
        float maxZ = bounds.max.z - spawnBounds.max.z - SpawnBoundsPadding;

        if (minX > maxX)
        {
            minX = bounds.min.x;
            maxX = bounds.max.x;
        }

        if (minZ > maxZ)
        {
            minZ = bounds.min.z;
            maxZ = bounds.max.z;
        }

        return new Vector3(Random.Range(minX, maxX), bounds.max.y, Random.Range(minZ, maxZ));
    }

    private bool IsPositionValid(Room room, Vector3 position, EnemySpawnVolume spawnVolume,
        out Vector3 finalPosition)
    {
        finalPosition = Vector3.zero;

        if (TryGetRoomGroundHit(room, position, out RaycastHit hit) == false)
            return false;

        if (NavMesh.SamplePosition(hit.point, out NavMeshHit navMeshHit, NavMeshSampleDistance,
                NavMesh.AllAreas) == false)
        {
            return false;
        }

        if (Mathf.Abs(navMeshHit.position.y - hit.point.y) > MaxGroundNavMeshHeightDifference)
        {
            return false;
        }

        if (TryGetRoomGroundHit(room, navMeshHit.position, out RaycastHit finalGroundHit) == false ||
            Mathf.Abs(finalGroundHit.point.y - navMeshHit.position.y) > MaxGroundNavMeshHeightDifference)
        {
            return false;
        }

        finalPosition = navMeshHit.position;
        return IsSpawnVolumeClear(spawnVolume, finalPosition, finalGroundHit);
    }

    private bool TryGetRoomGroundHit(Room room, Vector3 position, out RaycastHit hit)
    {
        Vector3 rayOrigin = position + Vector3.up * RayStartHeight;
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, RayDistance,
                _levelsConfiguration.GroundLayer, QueryTriggerInteraction.Ignore) == false)
        {
            return false;
        }

        return hit.collider != null &&
               (hit.collider.transform == room.transform || hit.collider.transform.IsChildOf(room.transform));
    }

    private bool IsSpawnVolumeClear(EnemySpawnVolume spawnVolume, Vector3 spawnPosition,
        RaycastHit supportingGroundHit)
    {
        int collisionMask = _levelsConfiguration.GroundLayer.value |
                            _levelsConfiguration.ObstacleLayer.value;
        if (collisionMask == 0)
            return true;

        for (int i = 0; i < spawnVolume.Shapes.Count; i++)
        {
            int overlapCount = spawnVolume.Shapes[i].OverlapNonAlloc(spawnPosition,
                collisionMask, _spawnOverlapResults);

            if (overlapCount >= _spawnOverlapResults.Length)
                return false;

            for (int overlapIndex = 0; overlapIndex < overlapCount; overlapIndex++)
            {
                Collider overlap = _spawnOverlapResults[overlapIndex];
                if (overlap == null)
                    continue;

                bool isGround = ContainsLayer(_levelsConfiguration.GroundLayer,
                    overlap.gameObject.layer);
                // The enemy collider can overlap its supporting slope at the contact point.
                // Only that surface is allowed; Ground geometry rising into the spawn volume is not.
                bool isSupportingSurface = overlap == supportingGroundHit.collider &&
                                           supportingGroundHit.point.y <=
                                           spawnPosition.y + GroundContactTolerance;
                if (isSupportingSurface)
                    continue;

                if (isGround && overlap.bounds.max.y <= spawnPosition.y + GroundContactTolerance)
                    continue;

                return false;
            }
        }

        return true;
    }

    private EnemySpawnVolume GetSpawnVolume(GameObject enemyPrefab)
    {
        if (enemyPrefab == null)
            throw new System.ArgumentNullException(nameof(enemyPrefab));

        if (_spawnVolumes.TryGetValue(enemyPrefab, out EnemySpawnVolume cachedVolume))
            return cachedVolume;

        var shapes = new List<EnemySpawnShape>();
        Transform prefabRoot = enemyPrefab.transform;
        Collider[] colliders = enemyPrefab.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || collider.enabled == false || collider.isTrigger ||
                IsTransformActiveInPrefab(collider.transform, prefabRoot) == false)
            {
                continue;
            }

            Matrix4x4 localToSpawn = GetLocalToSpawnMatrix(prefabRoot, collider.transform);
            if (TryCreateSpawnShape(collider, localToSpawn, out EnemySpawnShape shape))
                shapes.Add(shape);
        }

        if (shapes.Count == 0)
            AddNavMeshAgentFallback(enemyPrefab, shapes);

        if (shapes.Count == 0)
        {
            shapes.Add(EnemySpawnShape.CreateCapsule(
                Vector3.up * (FallbackSpawnHeight * 0.5f), Vector3.up,
                FallbackSpawnRadius, Mathf.Max(0f, FallbackSpawnHeight * 0.5f - FallbackSpawnRadius)));
        }

        var volume = new EnemySpawnVolume(shapes);
        _spawnVolumes.Add(enemyPrefab, volume);
        return volume;
    }

    private static void AddNavMeshAgentFallback(GameObject enemyPrefab,
        ICollection<EnemySpawnShape> shapes)
    {
        NavMeshAgent agent = enemyPrefab.GetComponentInChildren<NavMeshAgent>(true);
        if (agent == null || agent.enabled == false ||
            IsTransformActiveInPrefab(agent.transform, enemyPrefab.transform) == false)
        {
            return;
        }

        Matrix4x4 localToSpawn = GetLocalToSpawnMatrix(enemyPrefab.transform, agent.transform);
        Vector3 axis = localToSpawn.MultiplyVector(Vector3.up);
        float axisScale = axis.magnitude;
        if (axisScale <= Mathf.Epsilon)
            return;

        Vector3 right = localToSpawn.MultiplyVector(Vector3.right);
        Vector3 forward = localToSpawn.MultiplyVector(Vector3.forward);
        float radius = agent.radius * Mathf.Max(right.magnitude, forward.magnitude);
        float height = Mathf.Max(agent.height * axisScale, radius * 2f);
        Vector3 localCenter = Vector3.up * (agent.baseOffset + agent.height * 0.5f);
        Vector3 center = localToSpawn.MultiplyPoint3x4(localCenter);

        shapes.Add(EnemySpawnShape.CreateCapsule(center, axis / axisScale, radius,
            Mathf.Max(0f, height * 0.5f - radius)));
    }

    private static bool TryCreateSpawnShape(Collider collider, Matrix4x4 localToSpawn,
        out EnemySpawnShape shape)
    {
        switch (collider)
        {
            case SphereCollider sphere:
            {
                Vector3 center = localToSpawn.MultiplyPoint3x4(sphere.center);
                float scale = GetMaxAxisScale(localToSpawn);
                shape = EnemySpawnShape.CreateSphere(center, sphere.radius * scale);
                return true;
            }
            case CapsuleCollider capsule:
            {
                Vector3 localAxis = GetCapsuleAxis(capsule.direction);
                Vector3 axis = localToSpawn.MultiplyVector(localAxis);
                float axisScale = axis.magnitude;
                if (axisScale <= Mathf.Epsilon)
                    break;

                GetCapsulePerpendicularAxes(capsule.direction,
                    out Vector3 localPerpendicularA, out Vector3 localPerpendicularB);
                float radiusScale = Mathf.Max(
                    localToSpawn.MultiplyVector(localPerpendicularA).magnitude,
                    localToSpawn.MultiplyVector(localPerpendicularB).magnitude);
                float radius = capsule.radius * radiusScale;
                float height = Mathf.Max(capsule.height * axisScale, radius * 2f);
                Vector3 center = localToSpawn.MultiplyPoint3x4(capsule.center);

                shape = EnemySpawnShape.CreateCapsule(center, axis / axisScale, radius,
                    Mathf.Max(0f, height * 0.5f - radius));
                return true;
            }
            case CharacterController controller:
            {
                Vector3 axis = localToSpawn.MultiplyVector(Vector3.up);
                float axisScale = axis.magnitude;
                if (axisScale <= Mathf.Epsilon)
                    break;

                float radiusScale = Mathf.Max(
                    localToSpawn.MultiplyVector(Vector3.right).magnitude,
                    localToSpawn.MultiplyVector(Vector3.forward).magnitude);
                float radius = controller.radius * radiusScale;
                float height = Mathf.Max(controller.height * axisScale, radius * 2f);
                Vector3 center = localToSpawn.MultiplyPoint3x4(controller.center);

                shape = EnemySpawnShape.CreateCapsule(center, axis / axisScale, radius,
                    Mathf.Max(0f, height * 0.5f - radius));
                return true;
            }
            case BoxCollider box:
                shape = CreateBoundsShape(new Bounds(box.center, box.size), localToSpawn);
                return true;
            case MeshCollider mesh when mesh.sharedMesh != null:
                shape = CreateBoundsShape(mesh.sharedMesh.bounds, localToSpawn);
                return true;
        }

        shape = default;
        return false;
    }

    private static EnemySpawnShape CreateBoundsShape(Bounds localBounds,
        Matrix4x4 localToSpawn)
    {
        Vector3 min = localBounds.min;
        Vector3 max = localBounds.max;
        Vector3 firstCorner = localToSpawn.MultiplyPoint3x4(min);
        var transformedBounds = new Bounds(firstCorner, Vector3.zero);

        for (int x = 0; x < 2; x++)
        for (int y = 0; y < 2; y++)
        for (int z = 0; z < 2; z++)
        {
            Vector3 corner = new(
                x == 0 ? min.x : max.x,
                y == 0 ? min.y : max.y,
                z == 0 ? min.z : max.z);
            transformedBounds.Encapsulate(localToSpawn.MultiplyPoint3x4(corner));
        }

        return EnemySpawnShape.CreateBox(transformedBounds.center,
            transformedBounds.extents, Quaternion.identity);
    }

    private static Matrix4x4 GetLocalToSpawnMatrix(Transform prefabRoot,
        Transform child)
    {
        // EnemyFactory replaces the prefab root position/rotation, but preserves its scale
        // and all child-local transforms.
        Matrix4x4 childToRoot = prefabRoot.worldToLocalMatrix * child.localToWorldMatrix;
        return Matrix4x4.Scale(prefabRoot.localScale) * childToRoot;
    }

    private static bool IsTransformActiveInPrefab(Transform transform, Transform prefabRoot)
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.gameObject.activeSelf == false)
                return false;

            if (current == prefabRoot)
                return true;

            current = current.parent;
        }

        return false;
    }

    private static float GetMaxAxisScale(Matrix4x4 matrix) =>
        Mathf.Max(matrix.MultiplyVector(Vector3.right).magnitude,
            matrix.MultiplyVector(Vector3.up).magnitude,
            matrix.MultiplyVector(Vector3.forward).magnitude);

    private static Vector3 GetCapsuleAxis(int direction) => direction switch
    {
        0 => Vector3.right,
        2 => Vector3.forward,
        _ => Vector3.up
    };

    private static void GetCapsulePerpendicularAxes(int direction,
        out Vector3 firstAxis, out Vector3 secondAxis)
    {
        switch (direction)
        {
            case 0:
                firstAxis = Vector3.up;
                secondAxis = Vector3.forward;
                break;
            case 2:
                firstAxis = Vector3.right;
                secondAxis = Vector3.up;
                break;
            default:
                firstAxis = Vector3.right;
                secondAxis = Vector3.forward;
                break;
        }
    }

    private enum EnemySpawnShapeType
    {
        Sphere,
        Capsule,
        Box
    }

    private readonly struct EnemySpawnShape
    {
        private readonly EnemySpawnShapeType _type;
        private readonly Vector3 _center;
        private readonly Vector3 _axis;
        private readonly Vector3 _halfExtents;
        private readonly Quaternion _rotation;
        private readonly float _radius;
        private readonly float _segmentHalfLength;

        public Bounds Bounds { get; }

        private EnemySpawnShape(EnemySpawnShapeType type, Vector3 center, Vector3 axis,
            Vector3 halfExtents, Quaternion rotation, float radius, float segmentHalfLength,
            Bounds bounds)
        {
            _type = type;
            _center = center;
            _axis = axis;
            _halfExtents = halfExtents;
            _rotation = rotation;
            _radius = radius;
            _segmentHalfLength = segmentHalfLength;
            Bounds = bounds;
        }

        public static EnemySpawnShape CreateSphere(Vector3 center, float radius)
        {
            radius = Mathf.Max(0.01f, radius);
            return new EnemySpawnShape(EnemySpawnShapeType.Sphere, center, Vector3.up,
                Vector3.zero, Quaternion.identity, radius, 0f,
                new Bounds(center, Vector3.one * radius * 2f));
        }

        public static EnemySpawnShape CreateCapsule(Vector3 center, Vector3 axis,
            float radius, float segmentHalfLength)
        {
            radius = Mathf.Max(0.01f, radius);
            segmentHalfLength = Mathf.Max(0f, segmentHalfLength);
            axis = axis.sqrMagnitude > Mathf.Epsilon ? axis.normalized : Vector3.up;
            Vector3 segmentOffset = axis * segmentHalfLength;
            Vector3 min = Vector3.Min(center - segmentOffset, center + segmentOffset) -
                          Vector3.one * radius;
            Vector3 max = Vector3.Max(center - segmentOffset, center + segmentOffset) +
                          Vector3.one * radius;

            return new EnemySpawnShape(EnemySpawnShapeType.Capsule, center, axis,
                Vector3.zero, Quaternion.identity, radius, segmentHalfLength,
                new Bounds((min + max) * 0.5f, max - min));
        }

        public static EnemySpawnShape CreateBox(Vector3 center, Vector3 halfExtents,
            Quaternion rotation)
        {
            halfExtents = new Vector3(
                Mathf.Max(0.01f, halfExtents.x),
                Mathf.Max(0.01f, halfExtents.y),
                Mathf.Max(0.01f, halfExtents.z));
            Matrix4x4 rotationMatrix = Matrix4x4.Rotate(rotation);
            Vector3 boundsExtents = new(
                Mathf.Abs(rotationMatrix.m00) * halfExtents.x +
                Mathf.Abs(rotationMatrix.m01) * halfExtents.y +
                Mathf.Abs(rotationMatrix.m02) * halfExtents.z,
                Mathf.Abs(rotationMatrix.m10) * halfExtents.x +
                Mathf.Abs(rotationMatrix.m11) * halfExtents.y +
                Mathf.Abs(rotationMatrix.m12) * halfExtents.z,
                Mathf.Abs(rotationMatrix.m20) * halfExtents.x +
                Mathf.Abs(rotationMatrix.m21) * halfExtents.y +
                Mathf.Abs(rotationMatrix.m22) * halfExtents.z);

            return new EnemySpawnShape(EnemySpawnShapeType.Box, center, Vector3.up,
                halfExtents, rotation, 0f, 0f,
                new Bounds(center, boundsExtents * 2f));
        }

        public int OverlapNonAlloc(Vector3 spawnPosition, int collisionMask,
            Collider[] results)
        {
            Vector3 center = spawnPosition + _center;
            switch (_type)
            {
                case EnemySpawnShapeType.Sphere:
                    return Physics.OverlapSphereNonAlloc(center, _radius, results,
                        collisionMask, QueryTriggerInteraction.Ignore);
                case EnemySpawnShapeType.Capsule:
                    Vector3 segmentOffset = _axis * _segmentHalfLength;
                    return Physics.OverlapCapsuleNonAlloc(center - segmentOffset,
                        center + segmentOffset, _radius, results, collisionMask,
                        QueryTriggerInteraction.Ignore);
                case EnemySpawnShapeType.Box:
                    return Physics.OverlapBoxNonAlloc(center, _halfExtents, results,
                        _rotation, collisionMask, QueryTriggerInteraction.Ignore);
                default:
                    return 0;
            }
        }
    }

    private sealed class EnemySpawnVolume
    {
        public IReadOnlyList<EnemySpawnShape> Shapes { get; }
        public Bounds Bounds { get; }

        public EnemySpawnVolume(IReadOnlyList<EnemySpawnShape> shapes)
        {
            Shapes = shapes;
            Bounds bounds = shapes[0].Bounds;
            for (int i = 1; i < shapes.Count; i++)
                bounds.Encapsulate(shapes[i].Bounds);

            Bounds = bounds;
        }
    }

    private readonly struct PendingEnemySpawn
    {
        public GameObject Enemy { get; }
        public EnemySpawnVolume SpawnVolume { get; }
        public EnemyType EnemyType { get; }

        public PendingEnemySpawn(GameObject enemy, EnemySpawnVolume spawnVolume, EnemyType enemyType)
        {
            Enemy = enemy;
            SpawnVolume = spawnVolume;
            EnemyType = enemyType;
        }
    }

    private static bool ContainsLayer(LayerMask layerMask, int layer) =>
        (layerMask.value & (1 << layer)) != 0;

    private static float GetHorizontalArea(Collider collider) =>
        collider.bounds.size.x * collider.bounds.size.z;

}
