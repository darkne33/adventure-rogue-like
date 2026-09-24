using System;
using Features.Enemies.Scripts;
using Features.Enemies.Scripts.Level.Scripts;
using Features.Relics.Scripts;
using UnityEngine;
using Zenject;

namespace Features.Quests.Scripts
{
    public sealed class QuestRunTracker : ITickable, IDisposable
    {
        private const float SaveIntervalSeconds = 5f;

        private readonly QuestService _quests;
        private readonly RelicEventBus _events;
        private readonly IEnemiesProvider _enemies;
        private readonly EnemyRoomObserver _roomObserver;
        private readonly IRogueLikeRuntimeDataService _runtimeData;
        private readonly ICharacterProvider _characterProvider;
        private readonly ICharacterLevelService _characterLevel;
        private readonly CharacterWallet _wallet;
        private readonly UpgradeBuildService _build;
        private readonly ITimeScaleService _timeScale;
        private readonly IRoomTransitionService _roomTransition;
        private readonly RunRestartService _runRestart;
        private readonly RelicQuestRunProgress _relicProgress;

        private bool _isRunActive;
        private bool _isSilverBankingActive;
        private bool _isDisposed;
        private float _combatSeconds;
        private float _saveTimer;
        private int _recordedCombatSeconds;
        private int _runKills;
        private int _runRooms;
        private int _runRelics;
        private int _runGold;
        private int _previousGold;
        private int _previousSilver;

        public QuestRunTracker(QuestService quests, RelicEventBus events, IEnemiesProvider enemies,
            EnemyRoomObserver roomObserver, IRogueLikeRuntimeDataService runtimeData,
            ICharacterProvider characterProvider, ICharacterLevelService characterLevel,
            CharacterWallet wallet, UpgradeBuildService build, ITimeScaleService timeScale,
            IRoomTransitionService roomTransition, RunRestartService runRestart)
        {
            _quests = quests;
            _events = events;
            _enemies = enemies;
            _roomObserver = roomObserver;
            _runtimeData = runtimeData;
            _characterProvider = characterProvider;
            _characterLevel = characterLevel;
            _wallet = wallet;
            _build = build;
            _timeScale = timeScale;
            _roomTransition = roomTransition;
            _runRestart = runRestart;
            _relicProgress = new RelicQuestRunProgress(quests);

            _enemies.EnemyDefeated += HandleEnemyDefeated;
            _events.Hit += HandleHit;
            _events.ChestOpened += HandleChestOpened;
            _events.RelicCollected += HandleRelicCollected;
            _events.RoomStarted += HandleRoomStarted;
            _roomObserver.RoomCompleted += HandleRoomCompleted;
            _characterLevel.OnLevelUp += HandleLevelUp;
            _wallet.Gold.CountChanged += HandleGoldChanged;
            _wallet.Silver.CountChanged += HandleSilverChanged;
            _build.Changed += HandleBuildChanged;
            Application.focusChanged += HandleFocusChanged;
            Application.quitting += HandleQuitting;
        }

        public void BeginRun()
        {
            _quests.Flush();
            _combatSeconds = 0f;
            _saveTimer = 0f;
            _recordedCombatSeconds = 0;
            _runKills = 0;
            _runRooms = 0;
            _runRelics = 0;
            _runGold = 0;
            _previousGold = _wallet.Gold.Count;
            _previousSilver = _wallet.Silver.Count;
            _isSilverBankingActive = true;
            _isRunActive = true;
            _relicProgress.BeginRun(_runtimeData.CurrentRoomData, _characterProvider.CharacterFacade);
            _quests.RecordBest(QuestMetric.GoldHeld, _wallet.Gold.Count);

            HandleLevelUp(_characterLevel.GetLevel);
            HandleBuildChanged();
        }

        public void EndRun()
        {
            HandleSilverChanged(_wallet.Silver.Count);
            _isRunActive = false;
            _quests.Flush();
        }

        public void Tick()
        {
            if (_isRunActive == false)
                return;

            if (CanTrack() == false)
            {
                EndRun();
                return;
            }

            _saveTimer += Time.unscaledDeltaTime;
            if (_saveTimer >= SaveIntervalSeconds)
            {
                _saveTimer = 0f;
                _quests.Flush();
            }

            CharacterFacade character = _characterProvider.CharacterFacade;
            if (_timeScale.IsPaused || _roomTransition.IsPlaying || character.IsTransitionPaused ||
                Time.deltaTime <= 0f ||
                _runtimeData.CurrentRoomData is not DefaultEnemiesRoomData room ||
                room.IsCompleted || _roomObserver.IsRoomCompleted)
            {
                _relicProgress.SuspendMovement();
                return;
            }

            _combatSeconds += Time.deltaTime;
            _relicProgress.TickCombat(room, character, Time.deltaTime);
            int seconds = Mathf.FloorToInt(_combatSeconds);
            if (seconds <= _recordedCombatSeconds)
                return;

            _recordedCombatSeconds = seconds;
            _quests.RecordBest(QuestMetric.CombatSeconds, seconds);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            EndRun();
            _isSilverBankingActive = false;
            _enemies.EnemyDefeated -= HandleEnemyDefeated;
            _events.Hit -= HandleHit;
            _events.ChestOpened -= HandleChestOpened;
            _events.RelicCollected -= HandleRelicCollected;
            _events.RoomStarted -= HandleRoomStarted;
            _roomObserver.RoomCompleted -= HandleRoomCompleted;
            _characterLevel.OnLevelUp -= HandleLevelUp;
            _wallet.Gold.CountChanged -= HandleGoldChanged;
            _wallet.Silver.CountChanged -= HandleSilverChanged;
            _build.Changed -= HandleBuildChanged;
            Application.focusChanged -= HandleFocusChanged;
            Application.quitting -= HandleQuitting;
        }

        private bool CanTrack()
        {
            CharacterFacade character = _characterProvider.CharacterFacade;
            return _isRunActive && _runRestart.IsRestarting == false && character != null &&
                   character.HealthSystem != null && character.HealthSystem.IsDead == false;
        }

        private void HandleEnemyDefeated(CombatTarget target)
        {
            if (CanTrack() == false)
                return;

            _runKills++;
            _quests.RecordBest(QuestMetric.RunKills, _runKills);
            _quests.AddProgress(QuestMetric.TotalKills, 1);
            _relicProgress.RecordDefeat(_characterProvider.CharacterFacade, target, _combatSeconds);
        }

        private void HandleHit(RelicHitEvent hit)
        {
            if (!CanTrack() || hit.Attacker != _characterProvider.CharacterFacade)
                return;
            _relicProgress.RecordHit(hit);
            if (hit.Damage > 0 && hit.IsCritical)
                _quests.AddProgress(QuestMetric.TotalCriticalHits, 1);
        }

        private void HandleRoomStarted(RelicRoomEvent roomEvent)
        {
            if (CanTrack())
                _relicProgress.BeginRoom(roomEvent.RoomData, _characterProvider.CharacterFacade);
        }

        private void HandleRoomCompleted(DefaultEnemiesRoomData room)
        {
            if (CanTrack() == false)
                return;

            _runRooms++;
            _quests.RecordBest(QuestMetric.RunRoomsCleared, _runRooms);
            // A split boss can emit several kill events; clearing its room is one victory.
            if (room is BossRoomData)
                _quests.AddProgress(QuestMetric.TotalBossKills, 1);

            _relicProgress.CompleteRoom(room);

            _quests.Flush();
        }

        private void HandleChestOpened(Vector3 position)
        {
            if (CanTrack())
                _quests.AddProgress(QuestMetric.TotalChestsOpened, 1);
        }

        private void HandleRelicCollected(RelicDefinition relic)
        {
            if (CanTrack() == false || relic == null)
                return;

            _runRelics++;
            _quests.RecordBest(QuestMetric.RunRelicsCollected, _runRelics);
        }

        private void HandleLevelUp(int level)
        {
            if (CanTrack())
                _quests.RecordBest(QuestMetric.CharacterLevel, level);
        }

        private void HandleGoldChanged(int gold)
        {
            int gained = gold - _previousGold;
            _previousGold = gold;
            if (CanTrack() == false)
                return;

            _quests.RecordBest(QuestMetric.GoldHeld, gold);
            if (gained <= 0)
                return;

            _runGold += gained;
            _quests.RecordBest(QuestMetric.RunGoldCollected, _runGold);
            _quests.AddProgress(QuestMetric.TotalGoldCollected, gained);
        }

        private void HandleSilverChanged(int silver)
        {
            int gained = silver - _previousSilver;
            _previousSilver = silver;
            if (_isSilverBankingActive == false || gained <= 0)
                return;

            // This scene owns one run wallet. Keep banking collected rewards until disposal,
            // including a final pickup callback after death or the completion announcement.
            _quests.CreditSilver(gained);
        }

        private void HandleBuildChanged()
        {
            if (CanTrack() == false)
                return;

            int weaponLevel = 0;
            int scrollLevel = 0;
            int weaponsAtLevel3 = 0;
            int armorScrollLevel = 0;
            foreach (UpgradeBuildEntry entry in _build.SelectedUpgrades)
            {
                if (entry.Ability.Id == AbilityName.ArmorScroll)
                    armorScrollLevel = Math.Max(armorScrollLevel, entry.Level);
                if (entry.Ability is CharacterActiveAbility)
                {
                    weaponLevel = Math.Max(weaponLevel, entry.Level);
                    if (entry.Level >= 3)
                        weaponsAtLevel3++;
                }
                else if (entry.Ability is CharacterPassiveAbility)
                {
                    scrollLevel = Math.Max(scrollLevel, entry.Level);
                }
            }

            _quests.RecordBest(QuestMetric.WeaponLevel, weaponLevel);
            _quests.RecordBest(QuestMetric.ScrollLevel, scrollLevel);
            _quests.RecordBest(QuestMetric.WeaponsAtLevel3, weaponsAtLevel3);
            _quests.RecordBest(QuestMetric.ArmorScrollLevel, armorScrollLevel);
        }

        private void HandleFocusChanged(bool hasFocus)
        {
            if (hasFocus == false)
                _quests.Flush();
        }

        private void HandleQuitting() => EndRun();
    }
}
