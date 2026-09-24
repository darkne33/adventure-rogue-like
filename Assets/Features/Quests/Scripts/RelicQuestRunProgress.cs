using System.Collections.Generic;
using Features.Enemies.Scripts;
using Features.Relics.Scripts;
using UnityEngine;

namespace Features.Quests.Scripts
{
    // Run-local counters only. QuestRunTracker owns event subscriptions and filters inactive gameplay.
    internal sealed class RelicQuestRunProgress
    {
        private const float MaxMovementDelta = 15f;
        private const float MinimumMovingSpeed = 0.1f;
        private const float MaximumStationarySeconds = 2f;
        private const float BurstSeconds = 2f;
        private const float CloseKillDistance = 2f;
        private readonly QuestService _quests;
        private readonly Queue<float> _recentKills = new();

        private DefaultEnemiesRoomData _room;
        private Vector3 _previousPosition;
        private bool _hasPosition;
        private bool _roomHadCombat;
        private bool _enteredWithLowHealth;
        private bool _movementFailed;
        private float _stationarySeconds;
        private float _combatMeters;
        private int _hits;
        private int _closeKills;

        public RelicQuestRunProgress(QuestService quests) => _quests = quests;

        public void BeginRun(RoomData room, CharacterFacade character)
        {
            _combatMeters = 0f;
            _hits = 0;
            _closeKills = 0;
            BeginRoom(room, character);
        }

        public void BeginRoom(RoomData room, CharacterFacade character)
        {
            _recentKills.Clear();
            _room = room is DefaultEnemiesRoomData combatRoom && !combatRoom.IsCompleted ? combatRoom : null;
            _roomHadCombat = false;
            _movementFailed = false;
            _stationarySeconds = 0f;
            _hasPosition = false;
            _enteredWithLowHealth = _room != null && character != null &&
                character.HealthSystem.MaxHealth > 0f && character.HealthSystem.CurrentHealth > 0f &&
                character.HealthSystem.CurrentHealth / character.HealthSystem.MaxHealth <= 0.25f;
        }

        public void SuspendMovement() => _hasPosition = false;

        public void TickCombat(DefaultEnemiesRoomData room, CharacterFacade character, float deltaTime)
        {
            if (_room != room)
                BeginRoom(room, character);
            _roomHadCombat = true;
            Vector3 position = character.transform.position;
            if (!_hasPosition)
            {
                _previousPosition = position;
                _hasPosition = true;
                return;
            }

            Vector3 delta = position - _previousPosition;
            _previousPosition = position;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance > MaxMovementDelta)
                return;

            bool moving = distance >= MinimumMovingSpeed * deltaTime;
            _stationarySeconds = moving ? 0f : _stationarySeconds + deltaTime;
            if (_stationarySeconds > MaximumStationarySeconds)
                _movementFailed = true;
            if (moving)
            {
                _combatMeters += distance;
                _quests.RecordBest(QuestMetric.RunCombatMeters, Mathf.FloorToInt(_combatMeters));
            }
        }

        public void RecordHit(RelicHitEvent hit)
        {
            if (hit.Damage <= 0 || hit.AppliedDamage <= 0)
                return;
            _quests.RecordBest(QuestMetric.RunHits, ++_hits);
            // Use the full hit, including overkill, so small enemies do not cap heavy-hit progress.
            _quests.RecordBest(QuestMetric.MaxHitDamage, hit.Damage);
            _quests.RecordBest(QuestMetric.ProjectileDistinctTargets, hit.ProjectileDistinctTargets);
        }

        public void RecordDefeat(CharacterFacade character, CombatTarget target, float combatSeconds)
        {
            if (_room == null || target == null)
                return;
            _roomHadCombat = true;
            while (_recentKills.Count > 0 && combatSeconds - _recentKills.Peek() > BurstSeconds)
                _recentKills.Dequeue();
            _recentKills.Enqueue(combatSeconds);
            _quests.RecordBest(QuestMetric.KillsInTwoSeconds, _recentKills.Count);

            if ((target.transform.position - character.transform.position).sqrMagnitude <=
                CloseKillDistance * CloseKillDistance)
                _quests.RecordBest(QuestMetric.RunCloseRangeKills, ++_closeKills);
        }

        public void CompleteRoom(DefaultEnemiesRoomData room)
        {
            if (_room != room)
                return;
            if (_roomHadCombat && !_movementFailed)
                _quests.RecordBest(QuestMetric.MovingCombatRoomsCleared, 1);
            if (_roomHadCombat && _enteredWithLowHealth)
                _quests.RecordBest(QuestMetric.LowHealthEntryRoomsCleared, 1);
            _room = null;
            _recentKills.Clear();
            _hasPosition = false;
        }
    }
}
