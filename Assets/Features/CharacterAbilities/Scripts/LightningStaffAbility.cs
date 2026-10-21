using System;
using System.Collections.Generic;
using Features.Enemies.Scripts;
using Features.Relics.Scripts;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class LightningStaffAbility : CharacterActiveAbility, IDisposable
{
    private static readonly AbilityUpgradeType[] LightningUpgradeTypes =
    {
        AbilityUpgradeType.Damage,
        AbilityUpgradeType.AdditionalProjectiles,
        AbilityUpgradeType.LightningBounces,
        AbilityUpgradeType.ProjectileSize
    };

    private readonly IEnemiesProvider _enemies;
    private readonly CharacterDamageCalculator _damageCalculator;
    private readonly CharacterStats _stats;
    private readonly RelicEventBus _events;
    private readonly RelicManager _relics;
    private readonly IRogueLikeRuntimeDataService _runtimeData;
    private readonly LevelsConfiguration _levels;
    private readonly List<LightningBoltView> _bolts = new();
    private readonly List<LightningChain> _chains = new();
    private readonly List<CombatTarget> _strikeTargets = new();
    private LightningStaffAbilityConfiguration _configuration;
    private CharacterFacade _owner;
    private float _damage;
    private float _projectileCount;
    private float _bounceCount;
    private float _size;
    private float _attackRemaining;
    private float _nextBoltTime;
    private float _burstInterval;
    private float _chainInterval;
    private int _remainingStrikes;
    private int _sequence;
    private bool _attacking;
    private bool _observingRooms;

    public override AbilityUpgradeType[] UpgradeTypes => LightningUpgradeTypes;
    protected override bool StartCooldownImmediately => false;

    public LightningStaffAbility(IEnemiesProvider enemies, CharacterDamageCalculator damageCalculator,
        CharacterStats stats, RelicEventBus events, RelicManager relics,
        IRogueLikeRuntimeDataService runtimeData, LevelsConfiguration levels)
    {
        _enemies = enemies;
        _damageCalculator = damageCalculator;
        _stats = stats;
        _events = events;
        _relics = relics;
        _runtimeData = runtimeData;
        _levels = levels;
    }

    public override void Initialize(AbilityConfiguration abilityConfig)
    {
        Dispose();
        base.Initialize(abilityConfig);
        _configuration = (LightningStaffAbilityConfiguration)abilityConfig;
        Level = 0;
        _damage = Mathf.Max(1f, _configuration.StartDamage);
        _projectileCount = Mathf.Max(1, _configuration.StartProjectileCount);
        _bounceCount = Mathf.Max(0, _configuration.StartBounces);
        _size = 1f;
        Cooldown = Mathf.Max(0.01f, _configuration.EndCooldown);
        CurrentCooldown = 0f;
        StatName_1 = "Damage";
        StatName_2 = "Lightning Strikes";
        RefreshStats();
        if (_configuration.Prefab == null)
            Debug.LogError("Lightning Staff requires its prepared LightningBoltView prefab.", _configuration);
    }

    public override void OnEquip(CharacterStats characterStats)
    {
        base.OnEquip(characterStats);
        ApplyUpgradeEffect(CurrentPrimaryUpgrade);
        if (CurrentSecondaryUpgrade.HasValue)
            ApplyUpgradeEffect(CurrentSecondaryUpgrade.Value);
        if (!_observingRooms)
        {
            _runtimeData.RoomChanged += HandleRoomChanged;
            _observingRooms = true;
        }
        RefreshStats();
    }

    public override void OnUnequip(CharacterStats characterStats)
    {
        Dispose();
        base.OnUnequip(characterStats);
    }

    public override void RestartCooldown()
    {
        ClearAttack();
        base.RestartCooldown();
    }

    public override void Use(CharacterFacade character)
    {
        if (!IsAcquired || character == null || !character.isActiveAndEnabled || character.HealthSystem.IsDead)
        {
            ClearAttack();
            _owner = null;
            return;
        }
        if (_owner != character)
        {
            ClearAttack();
            _owner = character;
        }
        if (character.IsTransitionPaused || character.IsCinematicPaused)
            return;

        int sequence = _sequence;
        float deltaTime = Mathf.Max(0f, Time.deltaTime);
        TickBolts(deltaTime);
        TickChains(deltaTime);
        if (sequence != _sequence)
            return;
        if (_attacking)
            TickAttack(deltaTime);
        else
            base.Use(character);
        RefreshStats();
    }

    protected override bool IsReady(CharacterFacade character) =>
        IsAcquired && !_attacking && _configuration.Prefab != null && character != null &&
        FindInitialTarget(character, false) != null;

    protected override void OnUse(CharacterFacade character)
    {
        CombatTarget target = FindInitialTarget(character, true);
        if (target == null)
            return;
        int count = Mathf.Max(1, Mathf.FloorToInt(GetProjectileCount()));
        float timeMultiplier = GetModifiedCooldown() / Cooldown;
        _burstInterval = GetBurstInterval(count) * timeMultiplier;
        _chainInterval = Mathf.Max(0.01f, _configuration.BounceInterval) * timeMultiplier;
        _attackRemaining = Mathf.Max(0.01f, _configuration.AttackDuration) * timeMultiplier;
        _remainingStrikes = count - 1;
        _nextBoltTime = _burstInterval;
        _attacking = true;
        LaunchStrike(target);
    }

    private void TickAttack(float deltaTime)
    {
        int sequence = _sequence;
        _attackRemaining -= deltaTime;
        _nextBoltTime -= deltaTime;
        while (_remainingStrikes > 0 && _nextBoltTime <= 0f)
        {
            CombatTarget target = FindInitialTarget(_owner, true);
            if (target == null)
            {
                _remainingStrikes = 0;
                break;
            }
            _remainingStrikes--;
            _nextBoltTime += _burstInterval;
            LaunchStrike(target);
            if (sequence != _sequence)
                return;
        }
        if (_remainingStrikes == 0 && _attackRemaining <= 0f)
        {
            _attacking = false;
            StartCooldown();
        }
    }

    private void LaunchStrike(CombatTarget target)
    {
        int sequence = _sequence;
        var chain = new LightningChain
        {
            RemainingBounces = Mathf.Max(0, Mathf.FloorToInt(_bounceCount)),
            Delay = _chainInterval,
            Damage = _damage,
            Size = _size
        };
        Vector3 position = GetTargetPosition(target);
        Strike(target, position + Vector3.up * _configuration.SkyHeight, chain);
        if (sequence == _sequence && chain.RemainingBounces > 0)
            _chains.Add(chain);
    }

    private void TickChains(float deltaTime)
    {
        int sequence = _sequence;
        for (int index = _chains.Count - 1; index >= 0; index--)
        {
            LightningChain chain = _chains[index];
            chain.Delay -= deltaTime;
            while (chain.RemainingBounces > 0 && chain.Delay <= 0f)
            {
                CombatTarget target = FindBounceTarget(chain);
                if (target == null)
                {
                    chain.RemainingBounces = 0;
                    break;
                }
                chain.RemainingBounces--;
                chain.Delay += _chainInterval;
                chain.Damage *= Mathf.Clamp01(_configuration.BounceDamageMultiplier);
                Strike(target, chain.Origin, chain);
                if (sequence != _sequence)
                    return;
            }
            if (chain.RemainingBounces == 0)
                _chains.RemoveAt(index);
        }
    }

    private void Strike(CombatTarget target, Vector3 from, LightningChain chain)
    {
        if (!IsValidTarget(target))
            return;
        int sequence = _sequence;
        Vector3 position = GetTargetPosition(target);
        chain.Origin = position;
        chain.Visited.Add(target);
        LightningBoltView bolt = Object.Instantiate(_configuration.Prefab, _owner.transform, false);
        bolt.Initialize(from, position, chain.Size, _configuration.BoltLifetime);
        _bolts.Add(bolt);

        // Snapshot before callbacks change the enemy provider. Other bolts can hit these enemies again.
        _strikeTargets.Clear();
        _strikeTargets.Add(target);
        float radius = Mathf.Max(0.1f, _configuration.StrikeRadius) * chain.Size;
        float height = Mathf.Max(0.1f, _configuration.StrikeHeight) * chain.Size;
        IReadOnlyList<CombatTarget> enemies = _enemies.ActiveEnemies;
        for (int index = 0; index < enemies.Count; index++)
        {
            CombatTarget enemy = enemies[index];
            if (!IsValidTarget(enemy) || enemy == target)
                continue;
            Vector3 offset = enemy.AreaDamageBounds.ClosestPoint(position) - position;
            if (Mathf.Abs(offset.y) > height)
                continue;
            offset.y = 0f;
            if (offset.sqrMagnitude <= radius * radius)
                _strikeTargets.Add(enemy);
        }
        for (int index = 0; index < _strikeTargets.Count; index++)
        {
            if (_owner == null || _owner.HealthSystem.IsDead)
                break;
            ApplyDamage(_strikeTargets[index], chain.Damage, from);
            if (sequence != _sequence)
                return;
        }
        _strikeTargets.Clear();
    }

    private CombatTarget FindInitialTarget(CharacterFacade character, bool randomize)
    {
        if (character == null)
            return null;
        Vector3 origin = character.transform.position;
        Vector3 visionOrigin = origin + Vector3.up * _configuration.VisionHeightOffset;
        float rangeSqr = _configuration.TargetRange * _configuration.TargetRange;
        int obstacleMask = _levels.ObstacleLayer.value;
        CombatTarget result = null;
        int candidateCount = 0;
        foreach (CombatTarget enemy in _enemies.ActiveEnemies)
        {
            if (!IsValidTarget(enemy))
                continue;
            Vector3 position = GetTargetPosition(enemy);
            if ((position - origin).sqrMagnitude > rangeSqr ||
                Physics.Linecast(visionOrigin, position, obstacleMask, QueryTriggerInteraction.Ignore))
                continue;
            if (!randomize)
                return enemy;
            candidateCount++;
            if (UnityEngine.Random.Range(0, candidateCount) == 0)
                result = enemy;
        }
        return result;
    }

    private CombatTarget FindBounceTarget(LightningChain chain)
    {
        CombatTarget result = null;
        float distanceSqr = Mathf.Pow(_configuration.BounceRange * chain.Size, 2f);
        foreach (CombatTarget enemy in _enemies.ActiveEnemies)
        {
            if (!IsValidTarget(enemy) || chain.Visited.Contains(enemy))
                continue;
            float candidateDistance = (GetTargetPosition(enemy) - chain.Origin).sqrMagnitude;
            if (candidateDistance > distanceSqr)
                continue;
            distanceSqr = candidateDistance;
            result = enemy;
        }
        return result;
    }

    private void ApplyDamage(CombatTarget target, float damage, Vector3 from)
    {
        if (!IsValidTarget(target) || damage <= 0f)
            return;
        CharacterFacade owner = _owner;
        Vector3 position = target.transform.position;
        CharacterDamageResult roll = _damageCalculator.Calculate(Mathf.Max(1, Mathf.RoundToInt(damage)));
        int finalDamage = _relics.ModifyOutgoingDamage(roll.Damage, target);
        int applied = target.HealthSystem.GetDamage(finalDamage, roll.IsCritical);
        bool killed = target.IsDead;
        if (applied <= 0)
            return;
        if (!killed && target.Rigidbody != null && !target.Rigidbody.isKinematic)
        {
            Vector3 direction = position - from;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = position - owner.transform.position;
                direction.y = 0f;
            }
            if (direction.sqrMagnitude > 0.0001f)
                target.Rigidbody.AddForce(direction.normalized * _configuration.Knockback, ForceMode.Impulse);
        }
        float healed = owner.HealthSystem.IncreaseCurrentHealth(CharacterHealthRules.FromLegacyHealth(
            applied * Mathf.Max(0f, _stats.LifeSteal) * 0.01f));
        if (healed > 0f)
            _events.PublishHeal(new RelicHealEvent(owner, healed));
        target.EffectsSystem.DealDamage();
        _events.PublishHit(new RelicHitEvent(owner, target, finalDamage, roll.IsCritical,
            Id.ToString(), position, this, applied));
        if (killed)
            _events.PublishKill(new RelicKillEvent(owner, target, position, Id.ToString()));
    }

    private void TickBolts(float deltaTime)
    {
        for (int index = _bolts.Count - 1; index >= 0; index--)
        {
            LightningBoltView bolt = _bolts[index];
            if (bolt != null && bolt.Tick(deltaTime))
                continue;
            if (bolt != null)
                Object.Destroy(bolt.gameObject);
            _bolts.RemoveAt(index);
        }
    }

    public override float GetStatFromIncrease() => _damage;
    public override float GetStatToIncrease(float upgradeMultiplier) =>
        _damage + GetUpgradeValue(_configuration.DamageUpgradeIncrease, upgradeMultiplier);

    public override AbilityUpgradePreview[] GetAcquirePreviews() => new[]
    {
        new AbilityUpgradePreview("Damage", _configuration.StartDamage),
        new AbilityUpgradePreview("Lightning Strikes", _configuration.StartProjectileCount),
        new AbilityUpgradePreview("Bounces", _configuration.StartBounces)
    };

    public override AbilityUpgradePreview GetUpgradePreview(AbilityUpgradeEffect upgrade) =>
        upgrade.Type switch
        {
            AbilityUpgradeType.AdditionalProjectiles => new AbilityUpgradePreview("Lightning Strikes",
                GetProjectileCount(), GetProjectileCount() +
                GetUpgradeValue(_configuration.ProjectileCountUpgradeIncrease, upgrade.Value)),
            AbilityUpgradeType.LightningBounces => new AbilityUpgradePreview("Bounces", _bounceCount,
                _bounceCount + GetUpgradeValue(_configuration.BounceUpgradeIncrease, upgrade.Value)),
            AbilityUpgradeType.ProjectileSize => new AbilityUpgradePreview("Size", _size * 100f,
                (_size + GetUpgradeValue(_configuration.SizeUpgradeIncrease, upgrade.Value)) * 100f, "%"),
            _ => new AbilityUpgradePreview("Damage", _damage, GetStatToIncrease(upgrade.Value))
        };

    public override float CalculateEstimatedDps()
    {
        if (_configuration == null || _configuration.Prefab == null)
            return 0f;
        int count = Mathf.Max(1, Mathf.FloorToInt(GetProjectileCount()));
        float attackDuration = Mathf.Max(_configuration.AttackDuration, (count - 1) * GetBurstInterval(count));
        float cycle = attackDuration * GetModifiedCooldown() / Cooldown + GetModifiedCooldown();
        // Single-target estimate: splash and chains require other enemies.
        return _damageCalculator.CalculateAverageDamage(Mathf.Max(1, Mathf.RoundToInt(_damage))) * count /
               Mathf.Max(0.01f, cycle);
    }

    private void ApplyUpgradeEffect(AbilityUpgradeEffect upgrade)
    {
        switch (upgrade.Type)
        {
            case AbilityUpgradeType.Damage:
                _damage += GetUpgradeValue(_configuration.DamageUpgradeIncrease, upgrade.Value);
                break;
            case AbilityUpgradeType.AdditionalProjectiles:
                _projectileCount += GetUpgradeValue(_configuration.ProjectileCountUpgradeIncrease, upgrade.Value);
                break;
            case AbilityUpgradeType.LightningBounces:
                _bounceCount += GetUpgradeValue(_configuration.BounceUpgradeIncrease, upgrade.Value);
                break;
            case AbilityUpgradeType.ProjectileSize:
                _size += GetUpgradeValue(_configuration.SizeUpgradeIncrease, upgrade.Value);
                break;
        }
    }

    private float GetProjectileCount() => _projectileCount + Mathf.Max(0f, _stats.ProjectileCount);
    private float GetBurstInterval(int count) => Mathf.Max(_configuration.MinimumBurstInterval,
        Mathf.Min(_configuration.BurstInterval, _configuration.AttackDuration / Mathf.Max(1, count - 1)));

    private static bool IsValidTarget(CombatTarget target) => target != null && target.CanBeAttacked &&
        target.HealthSystem != null && !target.IsDead && target.gameObject.activeInHierarchy;
    private static Vector3 GetTargetPosition(CombatTarget target) => target.GetNextProjectileTarget().position;
    private void RefreshStats()
    {
        Stat_1 = _damage;
        Stat_2 = GetProjectileCount();
    }

    private void HandleRoomChanged(RoomData previous, RoomData current)
    {
        ClearAttack();
        CurrentCooldown = 0f;
    }

    private void ClearAttack()
    {
        _sequence++;
        _attacking = false;
        _remainingStrikes = 0;
        _chains.Clear();
        _strikeTargets.Clear();
        foreach (LightningBoltView bolt in _bolts)
        {
            if (bolt == null)
                continue;
            bolt.gameObject.SetActive(false);
            Object.Destroy(bolt.gameObject);
        }
        _bolts.Clear();
    }

    public void Dispose()
    {
        if (_observingRooms)
            _runtimeData.RoomChanged -= HandleRoomChanged;
        _observingRooms = false;
        ClearAttack();
        _owner = null;
    }

    private sealed class LightningChain
    {
        public readonly HashSet<CombatTarget> Visited = new();
        public Vector3 Origin;
        public int RemainingBounces;
        public float Delay;
        public float Damage;
        public float Size;
    }
}
