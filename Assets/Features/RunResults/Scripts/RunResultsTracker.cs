using System;
using System.Collections.Generic;
using Features.Enemies.Scripts;
using Features.Enemies.Scripts.Level.Scripts;
using Features.Relics.Scripts;
using Infrastructure.SaveSystem;
using UnityEngine;
using Zenject;

namespace Features.RunResults.Scripts
{
    public sealed class RunResultsTracker : ITickable, IDisposable
    {
        private readonly RelicEventBus _events;
        private readonly IEnemiesProvider _enemies;
        private readonly ICharacterProvider _characterProvider;
        private readonly ICharacterLevelService _characterLevel;
        private readonly CharacterWallet _wallet;
        private readonly UpgradeBuildService _build;
        private readonly ITimeScaleService _timeScale;
        private readonly IRoomTransitionService _roomTransition;
        private readonly RunRestartService _runRestart;
        private readonly IPlayerSaveLoadService _playerSaveLoad;
        private readonly IRogueLikeRuntimeDataService _runtimeData;
        private readonly List<WeaponStatistics> _weapons = new();
        private readonly Dictionary<AbilityName, WeaponStatistics> _weaponsById = new();

        private bool _isRunActive;
        private bool _isDisposed;
        private int _levelReached = 1;
        private int _enemiesDefeated;
        private int _goldEarned;
        private int _silverEarned;
        private int _previousGold;
        private int _previousSilver;
        private RunResultsData _capturedResults;

        public RunResultsTracker(RelicEventBus events, IEnemiesProvider enemies,
            ICharacterProvider characterProvider, ICharacterLevelService characterLevel,
            CharacterWallet wallet, UpgradeBuildService build, ITimeScaleService timeScale,
            IRoomTransitionService roomTransition, RunRestartService runRestart,
            IPlayerSaveLoadService playerSaveLoad, IRogueLikeRuntimeDataService runtimeData)
        {
            _events = events;
            _enemies = enemies;
            _characterProvider = characterProvider;
            _characterLevel = characterLevel;
            _wallet = wallet;
            _build = build;
            _timeScale = timeScale;
            _roomTransition = roomTransition;
            _runRestart = runRestart;
            _playerSaveLoad = playerSaveLoad;
            _runtimeData = runtimeData;

            _events.Hit += HandleHit;
            _enemies.EnemyDefeated += HandleEnemyDefeated;
            _characterLevel.OnLevelUp += HandleLevelUp;
            _wallet.Gold.CountChanged += HandleGoldChanged;
            _wallet.Silver.CountChanged += HandleSilverChanged;
            _build.Changed += HandleBuildChanged;
        }

        public void BeginRun()
        {
            if (_isDisposed)
                return;

            _weapons.Clear();
            _weaponsById.Clear();
            _capturedResults = null;
            _levelReached = Math.Max(1, _characterLevel.GetLevel);
            _enemiesDefeated = 0;
            _goldEarned = 0;
            _silverEarned = 0;
            _previousGold = _wallet.Gold.Count;
            _previousSilver = _wallet.Silver.Count;
            _isRunActive = true;
            RefreshWeaponBuild();
        }

        public RunResultsData CaptureResults()
        {
            if (_capturedResults != null)
                return _capturedResults;

            // Death has already set HealthSystem.IsDead when the results are captured.
            if (_isRunActive)
            {
                _levelReached = Math.Max(_levelReached, _characterLevel.GetLevel);
                RefreshWeaponBuild();
            }

            CompleteRun();
            var weapons = new List<RunWeaponResult>(_weapons.Count);
            foreach (WeaponStatistics weapon in _weapons)
            {
                weapons.Add(new RunWeaponResult(weapon.Ability, weapon.Level,
                    weapon.Damage, weapon.ActiveSeconds));
            }

            _capturedResults = new RunResultsData(_runtimeData.VisitedRoomsCount, _levelReached,
                _enemiesDefeated, _goldEarned, _silverEarned, weapons);
            return _capturedResults;
        }

        public void CompleteRun()
        {
            if (_isDisposed || !_isRunActive || _runRestart.IsRestarting)
                return;

            _isRunActive = false;
            _playerSaveLoad.RegisterCompletedRun();
        }

        public void Tick()
        {
            if (CanRecord() == false || _timeScale.IsPaused || _roomTransition.IsPlaying ||
                _characterProvider.CharacterFacade.IsTransitionPaused || Time.deltaTime <= 0f)
                return;

            float deltaTime = Time.deltaTime;
            foreach (WeaponStatistics weapon in _weapons)
            {
                if (weapon.IsEquipped)
                    weapon.ActiveSeconds += deltaTime;
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _isRunActive = false;
            _events.Hit -= HandleHit;
            _enemies.EnemyDefeated -= HandleEnemyDefeated;
            _characterLevel.OnLevelUp -= HandleLevelUp;
            _wallet.Gold.CountChanged -= HandleGoldChanged;
            _wallet.Silver.CountChanged -= HandleSilverChanged;
            _build.Changed -= HandleBuildChanged;
        }

        private bool CanRecord()
        {
            CharacterFacade character = _characterProvider.CharacterFacade;
            return _isRunActive && _runRestart.IsRestarting == false && character != null &&
                   character.HealthSystem != null && character.HealthSystem.IsDead == false;
        }

        private void HandleHit(RelicHitEvent hit)
        {
            if (CanRecord() == false || hit.Attacker != _characterProvider.CharacterFacade ||
                hit.SourceAbility == null || hit.AppliedDamage <= 0)
                return;

            WeaponStatistics weapon = GetOrAddWeapon(hit.SourceAbility);
            weapon.Damage += hit.AppliedDamage;
        }

        private void HandleEnemyDefeated(CombatTarget target)
        {
            if (CanRecord())
                _enemiesDefeated++;
        }

        private void HandleLevelUp(int level)
        {
            if (CanRecord())
                _levelReached = Math.Max(_levelReached, level);
        }

        private void HandleGoldChanged(int gold)
        {
            int gained = gold - _previousGold;
            _previousGold = gold;
            if (CanRecord() && gained > 0)
                _goldEarned += gained;
        }

        private void HandleSilverChanged(int silver)
        {
            int gained = silver - _previousSilver;
            _previousSilver = silver;
            if (CanRecord() && gained > 0)
                _silverEarned += gained;
        }

        private void HandleBuildChanged()
        {
            if (CanRecord())
                RefreshWeaponBuild();
        }

        private void RefreshWeaponBuild()
        {
            foreach (WeaponStatistics weapon in _weapons)
                weapon.IsEquipped = false;

            foreach (UpgradeBuildEntry entry in _build.SelectedUpgrades)
            {
                if (entry.Ability is not CharacterActiveAbility ability)
                    continue;

                WeaponStatistics weapon = GetOrAddWeapon(ability);
                weapon.Level = entry.Level;
                weapon.IsEquipped = true;
            }
        }

        private WeaponStatistics GetOrAddWeapon(CharacterActiveAbility ability)
        {
            if (_weaponsById.TryGetValue(ability.Id, out WeaponStatistics weapon))
                return weapon;

            weapon = new WeaponStatistics(ability);
            _weaponsById.Add(ability.Id, weapon);
            _weapons.Add(weapon);
            return weapon;
        }

        private sealed class WeaponStatistics
        {
            public CharacterActiveAbility Ability { get; }
            public int Level = 1;
            public long Damage;
            public float ActiveSeconds;
            public bool IsEquipped;

            public WeaponStatistics(CharacterActiveAbility ability)
            {
                Ability = ability;
                IsEquipped = true;
            }
        }
    }
}
