using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.Quests.Scripts
{
    public enum QuestMetric
    {
        CombatSeconds = 0,
        CharacterLevel = 1,
        RunKills = 2,
        TotalKills = 3,
        RunRoomsCleared = 4,
        TotalBossKills = 5,
        TotalChestsOpened = 6,
        RunRelicsCollected = 7,
        RunGoldCollected = 8,
        TotalGoldCollected = 9,
        WeaponLevel = 10,
        ScrollLevel = 11,
        WeaponsAtLevel3 = 12,
        TotalCriticalHits = 13,
        ArmorScrollLevel = 14,
        ProjectileDistinctTargets = 15,
        MovingCombatRoomsCleared = 16,
        RunCombatMeters = 17,
        RunHits = 18,
        GoldHeld = 19,
        KillsInTwoSeconds = 20,
        MaxHitDamage = 21,
        RunCloseRangeKills = 22,
        LowHealthEntryRoomsCleared = 23,
        TotalRunsCompleted = 24,
        TotalRoomsCleared = 25,
        SpecificAbilityLevel = 26,
        TotalHealthDamageTaken = 27
    }

    [Serializable]
    public sealed class QuestDefinition
    {
        [SerializeField] private string _id;
        [SerializeField] private bool _disabled;
        [SerializeField] private string _title;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private ProgressionCategory _category;
        [SerializeField] private QuestMetric _metric;
        [SerializeField, Min(1)] private int _target = 1;
        [SerializeField] private QuestMetric _secondaryMetric;
        [SerializeField, Min(0), Tooltip("Optional lifetime condition, independent of the main goal. Zero disables it.")]
        private int _secondaryTarget;
        [SerializeField, Tooltip("Optional ownership condition, used instead of the secondary metric.")]
        private string _secondaryUnlockId;
        [SerializeField, Min(0)] private int _silverReward;
        [SerializeField] private string[] _legacyQuestIds = Array.Empty<string>();
        [SerializeField] private string _characterId;
        [SerializeField] private AbilityConfiguration _ability;

        public string Id => _id;
        public bool IsDisabled => _disabled;
        public string Title => _title;
        public string Description => _description;
        public ProgressionCategory Category => _category;
        public QuestMetric Metric => _metric;
        public int Target => Mathf.Max(1, _target);
        public QuestMetric SecondaryMetric => _secondaryMetric;
        public int SecondaryTarget => Mathf.Max(0, _secondaryTarget);
        public string SecondaryUnlockId => _secondaryUnlockId;
        public bool HasSecondaryUnlockCondition => !string.IsNullOrWhiteSpace(SecondaryUnlockId);
        public bool HasSecondaryCondition => HasSecondaryUnlockCondition || SecondaryTarget > 0;
        public int DisplayTarget => HasSecondaryCondition ? 2 : Target;
        public int SilverReward => Mathf.Max(0, _silverReward);
        public IReadOnlyList<string> LegacyQuestIds => _legacyQuestIds ?? Array.Empty<string>();
        public string CharacterId => _characterId;
        public AbilityConfiguration Ability => _ability;
    }
}
