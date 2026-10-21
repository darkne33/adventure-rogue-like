using System;
using System.Collections.Generic;
using Features.Enemies.Scripts;
using Features.Relics.Scripts;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class AegisAbility : CharacterActiveAbility, IDisposable
{
    private static readonly AbilityUpgradeType[] AegisUpgradeTypes =
    {
        AbilityUpgradeType.Damage,
        AbilityUpgradeType.AegisCharges,
        AbilityUpgradeType.AegisRotationSpeed,
        AbilityUpgradeType.AegisSize
    };

    private readonly IEnemiesProvider _enemies;
    private readonly CharacterDamageCalculator _damageCalculator;
    private readonly CharacterStats _stats;
    private readonly RelicEventBus _events;
    private readonly RelicManager _relics;
    private readonly IRogueLikeRuntimeDataService _runtimeData;
    private readonly EnemyRoomObserver _roomObserver;
    private readonly List<Shockwave> _waves = new();
    private readonly List<CombatTarget> _targets = new();
    private AegisAbilityConfiguration _configuration;
    private CharacterFacade _owner;
    private AegisShieldView _view;
    private RoomData _room;
    private int _damage;
    private int _shieldCount;
    private int _additionalShields;
    private bool[] _activeSlots = Array.Empty<bool>();
    private float _rotationSpeed;
    private float _size;
    private bool _observingCombat;

    public override AbilityUpgradeType[] UpgradeTypes => AegisUpgradeTypes;
    public int ShieldCount => _shieldCount;
    public int MaxShields => Mathf.Max(1, _configuration.StartShieldCount + _additionalShields);

    public AegisAbility(IEnemiesProvider enemies, CharacterDamageCalculator damageCalculator,
        CharacterStats stats, RelicEventBus events, RelicManager relics,
        IRogueLikeRuntimeDataService runtimeData, EnemyRoomObserver roomObserver)
    {
        _enemies = enemies;
        _damageCalculator = damageCalculator;
        _stats = stats;
        _events = events;
        _relics = relics;
        _runtimeData = runtimeData;
        _roomObserver = roomObserver;
    }

    public override void Initialize(AbilityConfiguration abilityConfig)
    {
        Dispose();
        base.Initialize(abilityConfig);
        _configuration = (AegisAbilityConfiguration)abilityConfig;
        Level = 0;
        _damage = Mathf.Max(1, _configuration.StartDamage);
        _rotationSpeed = Mathf.Max(0f, _configuration.StartRotationSpeed);
        _additionalShields = 0;
        _activeSlots = Array.Empty<bool>();
        _size = 1f;
        _shieldCount = 0;
        Cooldown = 0f;
        CurrentCooldown = 0f;
        StatName_1 = "Damage";
        StatName_2 = "Shields";
        RefreshStats();
        if (_configuration.Prefab == null)
            Debug.LogError("Aegis requires its prepared AegisShieldView prefab.", _configuration);
    }

    public override void OnEquip(CharacterStats characterStats)
    {
        bool firstAcquisition = !IsAcquired;
        base.OnEquip(characterStats);
        ApplyUpgradeEffect(CurrentPrimaryUpgrade);
        if (CurrentSecondaryUpgrade.HasValue)
            ApplyUpgradeEffect(CurrentSecondaryUpgrade.Value);
        if (firstAcquisition)
        {
            RestoreShields();
            _roomObserver.RoomCompleted += HandleCombatCompleted;
            _observingCombat = true;
        }
        EnsureCapacity();
        RefreshStats();
        UpdateView();
    }

    public override void OnUnequip(CharacterStats characterStats)
    {
        Dispose();
        _shieldCount = 0;
        base.OnUnequip(characterStats);
    }

    // Only completion of a fight restores spent slots. There is no timed regeneration.
    public override void Use(CharacterFacade character)
    {
        if (!IsAcquired || character == null || character.HealthSystem.IsDead)
        {
            Cleanup();
            return;
        }

        EnsureOwner(character);
        if (_room != _runtimeData.CurrentRoomData)
        {
            _waves.Clear();
            _room = _runtimeData.CurrentRoomData;
            RecreateView();
        }

        EnsureCapacity();
        UpdateView();
        _view?.Tick(Time.deltaTime);
        TickShockwaves(Time.deltaTime);
        RefreshStats();
    }

    public bool TryBlockSegment(CharacterFacade character, CombatTarget source, Vector3 from,
        Vector3 to, float radius, out float hitDistance)
    {
        hitDistance = 0f;
        if (!PrepareBlock(character))
            return false;
        if (!_view.TryIntersectSegment(from, to, Mathf.Max(0f, radius), out int slot,
                out Vector3 hitPoint, out hitDistance))
            return false;
        ConsumeShield(slot, source, hitPoint);
        return true;
    }

    public bool TryBlockAttack(CharacterFacade character, CombatTarget source, Vector3? origin)
    {
        if (source == null || !PrepareBlock(character))
            return false;
        // Contact is tested against the enemy body; a ranged impact supplies its own origin instead.
        Collider body = !origin.HasValue ? source.GetComponent<Collider>() : null;
        if (body != null && _view.TryIntersectCollider(body, out int slot, out Vector3 hitPoint))
        {
            ConsumeShield(slot, source, hitPoint);
            return true;
        }
        Vector3 start = origin ?? (source.TargetToShootDamage != null
            ? source.TargetToShootDamage.position : source.transform.position);
        return TryBlockSegment(character, source, start, character.ProjectileSpawnPosition,
            0f, out _);
    }

    public bool TryBlockBox(CharacterFacade character, CombatTarget source, Vector3 center,
        Vector3 halfExtents, Quaternion rotation)
    {
        if (!PrepareBlock(character) || !_view.TryIntersectBox(center, halfExtents, rotation,
                out int slot, out Vector3 hitPoint))
            return false;
        ConsumeShield(slot, source, hitPoint);
        return true;
    }

    private bool PrepareBlock(CharacterFacade character)
    {
        if (!IsAcquired || _shieldCount <= 0 || character == null ||
            character.HealthSystem.IsDead || _configuration.Prefab == null)
            return false;
        EnsureOwner(character);
        EnsureCapacity();
        UpdateView();
        return _view != null;
    }

    private void ConsumeShield(int slot, CombatTarget source, Vector3 hitPoint)
    {
        if (slot < 0 || slot >= _activeSlots.Length || !_activeSlots[slot])
            return;
        _activeSlots[slot] = false;
        _shieldCount--;
        CharacterFacade character = _owner;
        Vector3 origin = character.ProjectileSpawnPosition;
        Vector3 direction = hitPoint - origin;
        if (direction.sqrMagnitude < 0.0001f && source != null)
            direction = source.transform.position - origin;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            direction = character.transform.forward;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.forward;

        float radius = _configuration.ShockwaveRadius;
        float duration = Mathf.Max(0.05f, _configuration.ShockwaveDuration * AbilityDurationMultiplier);
        _waves.Add(new Shockwave(origin, direction.normalized, radius, duration,
            Mathf.Cos(_configuration.ShockwaveAngle * 0.5f * Mathf.Deg2Rad),
            _configuration.ShockwaveHeight, GetBaseDamage(), _configuration.StartKnockback));
        UpdateView();
        _view?.PlayBlock(direction, radius, duration, _configuration.ShockwaveAngle);
        Sounds.Play(Features.Sounds.SoundId.PlayerBlock);
    }

    protected override void OnUse(CharacterFacade character) { }

    // Parry damage is conditional on a finite charge being hit, so it has no periodic DPS.
    public override float CalculateEstimatedDps() => 0f;
    public override void RestartCooldown() { }

    public override float GetStatFromIncrease() => _damage;
    public override float GetStatToIncrease(float upgradeMultiplier) =>
        _damage + GetDamageIncrease(upgradeMultiplier);

    public override AbilityUpgradePreview[] GetAcquirePreviews() => new[]
    {
        new AbilityUpgradePreview("Damage", _damage),
        new AbilityUpgradePreview("Shields", MaxShields)
    };

    public override AbilityUpgradePreview GetUpgradePreview(AbilityUpgradeEffect upgrade) =>
        upgrade.Type switch
        {
            AbilityUpgradeType.AegisCharges => new AbilityUpgradePreview("Shield Capacity",
                MaxShields, MaxShields + 1),
            AbilityUpgradeType.AegisRotationSpeed => new AbilityUpgradePreview("Rotation Speed",
                _rotationSpeed, _rotationSpeed + GetUpgradeValue(_configuration.RotationSpeedUpgradeIncrease,
                    upgrade.Value), "deg/s"),
            AbilityUpgradeType.AegisSize => new AbilityUpgradePreview("Shield Size", _size * 100f,
                (_size + GetUpgradeValue(_configuration.SizeUpgradeIncrease, upgrade.Value)) * 100f, "%"),
            _ => new AbilityUpgradePreview("Damage", _damage, _damage + GetDamageIncrease(upgrade.Value))
        };

    private void ApplyUpgradeEffect(AbilityUpgradeEffect upgrade)
    {
        switch (upgrade.Type)
        {
            case AbilityUpgradeType.Damage:
                _damage += GetDamageIncrease(upgrade.Value);
                break;
            case AbilityUpgradeType.AegisCharges:
                _additionalShields++;
                break;
            case AbilityUpgradeType.AegisRotationSpeed:
                _rotationSpeed += GetUpgradeValue(_configuration.RotationSpeedUpgradeIncrease, upgrade.Value);
                break;
            case AbilityUpgradeType.AegisSize:
                _size += GetUpgradeValue(_configuration.SizeUpgradeIncrease, upgrade.Value);
                break;
        }
    }

    private void EnsureOwner(CharacterFacade character)
    {
        if (_owner == character && _view != null)
            return;
        Cleanup();
        _owner = character;
        _room = _runtimeData.CurrentRoomData;
        RecreateView();
    }

    private void RecreateView()
    {
        if (_view != null)
        {
            _view.Cleanup();
            Object.Destroy(_view.gameObject);
        }
        _view = null;
        if (_owner == null || _configuration.Prefab == null)
            return;
        _view = Object.Instantiate(_configuration.Prefab, _owner.transform, false);
        _view.Initialize(_owner);
        UpdateView();
    }

    private void TickShockwaves(float deltaTime)
    {
        // Callbacks may remove enemies from the provider, so snapshot it before applying any hit.
        _targets.Clear();
        _targets.AddRange(_enemies.ActiveEnemies);
        for (int waveIndex = _waves.Count - 1; waveIndex >= 0; waveIndex--)
        {
            Shockwave wave = _waves[waveIndex];
            wave.Elapsed += Mathf.Max(0f, deltaTime);
            float radius = Mathf.Lerp(0.1f, wave.Radius, Mathf.Clamp01(wave.Elapsed / wave.Duration));
            foreach (CombatTarget target in _targets)
            {
                if (target == null || !target.CanBeAttacked || target.IsDead ||
                    !target.gameObject.activeInHierarchy || wave.HitTargets.Contains(target))
                    continue;

                Vector3 offset = target.AreaDamageBounds.ClosestPoint(wave.Origin) - wave.Origin;
                if (Mathf.Abs(offset.y) > wave.Height)
                    continue;
                offset.y = 0f;
                if (offset.sqrMagnitude > radius * radius ||
                    (offset.sqrMagnitude > 0.0001f && Vector3.Dot(wave.Direction, offset.normalized) < wave.HalfAngleCos))
                    continue;

                wave.HitTargets.Add(target);
                ApplyDamage(target, wave);
            }

            if (wave.Elapsed >= wave.Duration)
                _waves.RemoveAt(waveIndex);
        }
        _targets.Clear();
    }

    private void ApplyDamage(CombatTarget target, Shockwave wave)
    {
        CharacterDamageResult roll = _damageCalculator.Calculate(wave.Damage);
        int finalDamage = _relics.ModifyOutgoingDamage(roll.Damage, target);
        Vector3 position = target.transform.position;
        int applied = target.HealthSystem.GetDamage(finalDamage, roll.IsCritical);
        bool killed = target.IsDead;
        if (applied <= 0)
            return;

        if (!killed && target.Rigidbody != null && !target.Rigidbody.isKinematic)
        {
            Vector3 push = target.transform.position - wave.Origin;
            push.y = 0f;
            if (push.sqrMagnitude < 0.0001f)
                push = wave.Direction;
            target.Rigidbody.AddForce(push.normalized * wave.Knockback, ForceMode.Impulse);
        }
        float healed = _owner.HealthSystem.IncreaseCurrentHealth(CharacterHealthRules.FromLegacyHealth(
            applied * Mathf.Max(0f, _stats.LifeSteal) * 0.01f));
        if (healed > 0f)
            _events.PublishHeal(new RelicHealEvent(_owner, healed));
        target.EffectsSystem.DealDamage();
        _events.PublishHit(new RelicHitEvent(_owner, target, finalDamage, roll.IsCritical,
            Id.ToString(), position, this, applied));
        if (killed)
            _events.PublishKill(new RelicKillEvent(_owner, target, position, Id.ToString()));
    }

    private int GetBaseDamage() => Mathf.Max(1, _damage + Mathf.RoundToInt(Mathf.Max(0f, _stats.ThornsDamage)));
    private int GetDamageIncrease(float multiplier) =>
        Mathf.Max(0, Mathf.RoundToInt(GetUpgradeValue(_configuration.DamageUpgradeIncrease, multiplier)));
    private void EnsureCapacity()
    {
        // A new capacity slot remains empty until the end of the fight.
        if (_activeSlots.Length != MaxShields)
            Array.Resize(ref _activeSlots, MaxShields);
    }

    private void RestoreShields()
    {
        EnsureCapacity();
        for (int index = 0; index < _activeSlots.Length; index++)
            _activeSlots[index] = true;
        _shieldCount = _activeSlots.Length;
        UpdateView();
    }

    private void HandleCombatCompleted(DefaultEnemiesRoomData room)
    {
        if (IsAcquired && _owner != null && !_owner.HealthSystem.IsDead &&
            ReferenceEquals(room, _runtimeData.CurrentRoomData))
            RestoreShields();
    }

    private void UpdateView()
    {
        if (_view == null)
            return;
        _view.Configure(_size, _rotationSpeed);
        _view.SetShieldSlots(MaxShields, _activeSlots);
    }

    public void Dispose()
    {
        if (_observingCombat)
            _roomObserver.RoomCompleted -= HandleCombatCompleted;
        _observingCombat = false;
        Cleanup();
    }

    private void RefreshStats()
    {
        Stat_1 = _damage;
        Stat_2 = MaxShields;
    }

    private void Cleanup()
    {
        _waves.Clear();
        _targets.Clear();
        if (_view != null)
        {
            _view.Cleanup();
            _view.gameObject.SetActive(false);
            Object.Destroy(_view.gameObject);
        }
        _view = null;
        _owner = null;
        _room = null;
    }

    private sealed class Shockwave
    {
        public readonly Vector3 Origin;
        public readonly Vector3 Direction;
        public readonly float Radius;
        public readonly float Duration;
        public readonly float HalfAngleCos;
        public readonly float Height;
        public readonly int Damage;
        public readonly float Knockback;
        public readonly HashSet<CombatTarget> HitTargets = new();
        public float Elapsed;

        public Shockwave(Vector3 origin, Vector3 direction, float radius, float duration,
            float halfAngleCos, float height, int damage, float knockback)
        {
            Origin = origin;
            Direction = direction;
            Radius = radius;
            Duration = duration;
            HalfAngleCos = halfAngleCos;
            Height = height;
            Damage = damage;
            Knockback = knockback;
        }
    }
}
