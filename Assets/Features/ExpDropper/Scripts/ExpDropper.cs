using UnityEngine;
using Zenject;

public sealed class ExpDropper
{
    private readonly ExpDropperConfiguration _configuration;
    private readonly ICharacterProvider _characterProvider;
    private readonly ICharacterLevelService _characterLevelService;
    private readonly DiContainer _container;
    private int _combatRoomsPerUpgrade;
    private int _remainingCombatRooms;

    public ExpDropper(ExpDropperConfiguration configuration, ICharacterProvider characterProvider,
        ICharacterLevelService characterLevelService, DiContainer container)
    {
        _configuration = configuration;
        _characterProvider = characterProvider;
        _characterLevelService = characterLevelService;
        _container = container;
    }

    public float GetExperienceMultiplier(EnemyRank rank) =>
        rank == EnemyRank.Elite
            ? Mathf.Max(1f, _configuration != null ? _configuration.EliteExperienceMultiplier : 2f)
            : 1f;

    public double BossLevelProgress =>
        Mathf.Max(0f, _configuration != null ? _configuration.BossLevelProgress : 1f);

    internal RoomExperienceBudget CreateRoomExperienceBudget(int enemyCount, float xpBonus,
        int combatRoomIndex)
    {
        if (combatRoomIndex == 0)
        {
            _remainingCombatRooms = 0;
            return new RoomExperienceBudget(1d, enemyCount);
        }

        int earlyCombatRoomCount = Mathf.Max(1,
            _configuration != null ? _configuration.EarlyCombatRoomCount : 10);
        bool isEarlyCombatRoom = combatRoomIndex < earlyCombatRoomCount;

        // The first room after the early phase starts a new cycle immediately.
        if (_remainingCombatRooms <= 0 || combatRoomIndex == earlyCombatRoomCount)
        {
            int minRooms = Mathf.Max(1,
                _configuration != null ? _configuration.MinCombatRoomsPerUpgrade : 1);
            int maxRooms = isEarlyCombatRoom ? 2 : 3;
            if (_configuration != null)
                maxRooms = isEarlyCombatRoom
                    ? _configuration.EarlyMaxCombatRoomsPerUpgrade
                    : _configuration.MaxCombatRoomsPerUpgrade;

            _combatRoomsPerUpgrade = Random.Range(minRooms, Mathf.Max(minRooms, maxRooms) + 1);
            _remainingCombatRooms = _combatRoomsPerUpgrade;
        }

        _remainingCombatRooms--;
        double progress = (1d + Mathf.Max(0f, xpBonus) / 100d) / _combatRoomsPerUpgrade;
        return new RoomExperienceBudget(progress, enemyCount);
    }

    public void DropLevelProgress(Vector3 position, double levelProgress)
    {
        if (levelProgress <= 0d)
            return;

        if (_configuration == null || _configuration.ExpRupeePrefab == null)
        {
            _characterLevelService.AddLevelProgress(levelProgress);
            return;
        }

        CreateRupee(position).ConstructLevelProgress(levelProgress, _configuration,
            _characterProvider, _characterLevelService, position + GetBurstOffset());
    }

    public void DropExp(Vector3 position, int amount)
    {
        if (amount <= 0)
            return;

        if (_configuration == null || _configuration.ExpRupeePrefab == null)
        {
            _characterLevelService.AddExp(amount);
            return;
        }

        CreateRupee(position).Construct(amount, _configuration, _characterProvider, _characterLevelService,
            position + GetBurstOffset());
    }

    private ExpRupee CreateRupee(Vector3 position)
    {
        GameObject rupeeObject = _container.InstantiatePrefab(_configuration.ExpRupeePrefab, position,
            Quaternion.identity, null);
        ExpRupee expRupee = rupeeObject.GetComponent<ExpRupee>();
        if (expRupee != null)
            return expRupee;

        Object.Destroy(rupeeObject);
        throw new System.InvalidOperationException("The experience pickup prefab must contain ExpRupee.");
    }

    private Vector3 GetBurstOffset()
    {
        Vector2 scatter = Random.insideUnitCircle * Mathf.Max(0f, _configuration.BurstScatterRadius);
        return new Vector3(scatter.x, Mathf.Max(0f, _configuration.BurstHeight), scatter.y);
    }

    internal sealed class RoomExperienceBudget
    {
        private double _remainingProgress;

        public double EnemyShare { get; }
        public bool IsSpawningFinished { get; private set; }

        public RoomExperienceBudget(double progress, int enemyCount)
        {
            _remainingProgress = System.Math.Max(0d, progress);
            EnemyShare = _remainingProgress / Mathf.Max(1, enemyCount);
        }

        public void FinishSpawning() => IsSpawningFinished = true;

        public double Claim(double share, bool isRoomCleared)
        {
            // Hazards and unspawned slots leave their share for the final combat enemy.
            double reward = isRoomCleared
                ? _remainingProgress
                : System.Math.Min(_remainingProgress, System.Math.Max(0d, share));
            _remainingProgress -= reward;
            return reward;
        }
    }
}
