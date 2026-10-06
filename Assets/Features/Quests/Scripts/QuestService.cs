using System;
using System.Collections.Generic;
using Features.Relics.Scripts;
using Newtonsoft.Json;
using UnityEngine;
using Zenject;

namespace Features.Quests.Scripts
{
    public sealed class QuestService : ITickable, IDisposable
    {
        private const string SaveKey = "little_rush.quests.v1";
        private const float SaveInterval = 10f;
        private readonly PlayerWallet _wallet;
        private readonly Dictionary<QuestMetric, int> _progress = new();
        private readonly Dictionary<string, int> _questProgress = new();
        private readonly HashSet<string> _activeQuests = new();
        private readonly HashSet<string> _completed = new();
        private readonly HashSet<string> _purchased = new();
        private readonly HashSet<string> _claimedRewards = new();
        private readonly HashSet<string> _viewedUnlocks = new();
        private bool _dirty;
        private bool _updatingWallet;
        private bool _isRunActive;
        private float _saveTimer;

        public event Action Changed;
        public event Action<QuestDefinition> QuestCompleted;

        public ProgressionConfiguration Configuration { get; private set; }
        public IReadOnlyList<QuestDefinition> Definitions => Configuration != null
            ? Configuration.Quests : Array.Empty<QuestDefinition>();
        public IReadOnlyList<UnlockDefinition> Unlocks => Configuration != null
            ? Configuration.Unlocks : Array.Empty<UnlockDefinition>();
        public int TotalCount => Definitions.Count;
        public int Silver => _wallet.Silver.Count;
        public int CompletedRuns => GetMetric(QuestMetric.TotalRunsCompleted);

        public bool HasClaimableRewards
        {
            get
            {
                foreach (QuestDefinition quest in Definitions)
                    if (CanClaimReward(quest.Id))
                        return true;
                return false;
            }
        }

        public bool HasNewUnlocks
        {
            get
            {
                foreach (UnlockDefinition unlock in Unlocks)
                    if (IsNewUnlock(unlock))
                        return true;
                return false;
            }
        }

        public int CompletedCount
        {
            get
            {
                int count = 0;
                foreach (QuestDefinition quest in Definitions)
                    if (IsCompleted(quest.Id))
                        count++;
                return count;
            }
        }

        public QuestService(PlayerWallet wallet) => _wallet = wallet;

        public void Initialize(ProgressionConfiguration configuration)
        {
            if (Configuration != null)
                return;
            Configuration = configuration != null ? configuration
                : throw new ArgumentNullException(nameof(configuration));
            Load();
            RestoreLegacyQuestCompletions();
            _wallet.Silver.CountChanged += HandleSilverChanged;
            Application.quitting += Flush;
            Application.focusChanged += HandleFocusChanged;
            CompleteEligibleQuests();
            Flush();
        }

        public void BeginRun(string characterId)
        {
            if (_isRunActive)
                return;

            _isRunActive = true;
            _activeQuests.Clear();
            // Snapshot eligibility: later steps start in a new run, never midway through this one.
            foreach (QuestDefinition quest in Definitions)
                if (!IsCompleted(quest.Id) && ArePrerequisitesMet(quest) &&
                    (string.IsNullOrWhiteSpace(quest.CharacterId) || quest.CharacterId == characterId))
                    _activeQuests.Add(quest.Id);
        }

        public void EndRun(bool clearedCombatRoom)
        {
            if (!_isRunActive)
                return;

            if (clearedCombatRoom)
                AddProgress(QuestMetric.TotalRunsCompleted, 1);
            _isRunActive = false;
            _activeQuests.Clear();
            Flush();
        }

        public bool IsCompleted(string questId) =>
            !string.IsNullOrWhiteSpace(questId) && _completed.Contains(questId);

        public bool IsRewardClaimed(string questId) =>
            !string.IsNullOrWhiteSpace(questId) && _claimedRewards.Contains(questId);

        public bool CanClaimReward(string questId) => IsCompleted(questId) &&
            !IsRewardClaimed(questId) && GetQuest(questId) is { SilverReward: > 0 };

        public bool TryClaimReward(string questId)
        {
            if (!CanClaimReward(questId))
                return false;

            // Record the claim before crediting the wallet; both are flushed in one snapshot.
            _claimedRewards.Add(questId);
            _dirty = true;
            CreditSilver(GetQuest(questId).SilverReward);
            return true;
        }

        public bool IsNewUnlock(UnlockDefinition unlock) => unlock != null &&
            !string.IsNullOrWhiteSpace(unlock.Id) && !IsOwned(unlock) &&
            IsRequirementMet(unlock) && !_viewedUnlocks.Contains(unlock.Id);

        public void MarkUnlockViewed(UnlockDefinition unlock)
        {
            if (!IsNewUnlock(unlock) || !_viewedUnlocks.Add(unlock.Id))
                return;

            _dirty = true;
            Flush();
            Changed?.Invoke();
        }

        public QuestDefinition GetQuest(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId))
                return null;
            foreach (QuestDefinition quest in Definitions)
                if (quest.Id == questId)
                    return quest;
            return null;
        }

        public UnlockDefinition GetUnlock(string unlockId)
        {
            foreach (UnlockDefinition unlock in Unlocks)
                if (unlock.Id == unlockId)
                    return unlock;
            return null;
        }

        public string GetQuestDescription(QuestDefinition quest)
        {
            if (quest == null)
                return string.Empty;
            if (IsCompleted(quest.Id))
                return quest.Description;

            var requirements = new List<string>();
            if (CompletedRuns < quest.MinimumCompletedRuns)
                requirements.Add($"Finish {quest.MinimumCompletedRuns} runs ({CompletedRuns}/{quest.MinimumCompletedRuns})");
            foreach (string questId in quest.RequiredQuestIds)
                if (!IsCompleted(questId))
                    requirements.Add($"Complete {GetQuest(questId)?.Title ?? questId}");
            foreach (string unlockId in quest.RequiredUnlockIds)
            {
                UnlockDefinition unlock = GetUnlock(unlockId);
                if (!IsOwned(unlock))
                    requirements.Add($"Own {unlock?.DisplayName ?? unlockId}");
            }

            if (requirements.Count > 0)
            {
                string remaining = requirements.Count > 1 ? $" (+{requirements.Count - 1} more)" : string.Empty;
                return $"{quest.Description}\nNext: {requirements[0]}{remaining}.";
            }
            return quest.HasPrerequisites
                ? $"{quest.Description}\nStarts next eligible run."
                : quest.Description;
        }

        public UnlockDefinition GetUnlockForQuest(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId))
                return null;
            foreach (UnlockDefinition unlock in Unlocks)
                if (unlock.RequiredQuestId == questId)
                    return unlock;
            return null;
        }

        public bool IsOwned(UnlockDefinition unlock) => unlock != null &&
            (unlock.UnlockedByDefault || IsDefaultContent(unlock) || _purchased.Contains(unlock.Id));

        public bool IsRequirementMet(UnlockDefinition unlock) => unlock != null &&
            (string.IsNullOrWhiteSpace(unlock.RequiredQuestId) ||
             GetQuest(unlock.RequiredQuestId) != null && IsCompleted(unlock.RequiredQuestId));

        public bool CanPurchase(UnlockDefinition unlock) => unlock != null &&
            !string.IsNullOrWhiteSpace(unlock.Id) && !IsOwned(unlock) &&
            IsRequirementMet(unlock) && Silver >= unlock.SilverCost;

        public bool TryPurchase(string unlockId)
        {
            foreach (UnlockDefinition unlock in Unlocks)
            {
                if (unlock.Id != unlockId || !CanPurchase(unlock))
                    continue;

                // Ownership and payment are one save snapshot, including a zero-cost purchase.
                _purchased.Add(unlock.Id);
                _dirty = true;
                _updatingWallet = true;
                try
                {
                    _wallet.Silver.Remove(unlock.SilverCost);
                }
                finally
                {
                    _updatingWallet = false;
                }
                Flush();
                Changed?.Invoke();
                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        public void CompleteAllQuestsAndUnlockAllForEditor()
        {
            if (Configuration == null)
                throw new InvalidOperationException("Quest service must be initialized before completing progression.");

            foreach (QuestDefinition quest in Definitions)
            {
                if (quest == null || string.IsNullOrWhiteSpace(quest.Id))
                    continue;

                if (GetQuestProgress(quest.Id) != quest.Target)
                {
                    _questProgress[quest.Id] = quest.Target;
                    _dirty = true;
                }
                _dirty |= _completed.Add(quest.Id);
            }

            foreach (UnlockDefinition unlock in Unlocks)
                if (unlock != null && !string.IsNullOrWhiteSpace(unlock.Id))
                    _dirty |= _purchased.Add(unlock.Id);

            // Keep normal CLAIM rewards and wallet balances; avoid queuing every completion notification.
            Flush();
            Changed?.Invoke();
        }
#endif

        public bool IsCharacterOwned(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
                return false;
            foreach (string id in Configuration.DefaultCharacters)
                if (id == characterId)
                    return true;
            foreach (UnlockDefinition unlock in Unlocks)
                if (unlock.Category == ProgressionCategory.Characters &&
                    unlock.CharacterId == characterId && IsOwned(unlock))
                    return true;
            return false;
        }

        public bool IsAbilityOwned(AbilityName ability)
        {
            foreach (AbilityName id in Configuration.DefaultAbilities)
                if (id == ability)
                    return true;
            foreach (UnlockDefinition unlock in Unlocks)
                if ((unlock.Category == ProgressionCategory.Weapons || unlock.Category == ProgressionCategory.Scrolls) &&
                    unlock.Ability != null && unlock.Ability.AbilityName == ability && IsOwned(unlock))
                    return true;
            return false;
        }

        public bool IsRelicOwned(RelicDefinition relic)
        {
            if (relic == null)
                return false;
            foreach (RelicDefinition available in Configuration.DefaultRelics)
                if (available != null && available.Id == relic.Id)
                    return true;
            foreach (UnlockDefinition unlock in Unlocks)
                if (unlock.Category == ProgressionCategory.Relics && unlock.Relic != null &&
                    unlock.Relic.Id == relic.Id && IsOwned(unlock))
                    return true;
            return false;
        }

        public void CreditSilver(int amount)
        {
            if (amount <= 0)
                return;
            _updatingWallet = true;
            try
            {
                _wallet.Silver.Set((int)Math.Min(int.MaxValue, (long)Silver + amount));
            }
            finally
            {
                _updatingWallet = false;
            }
            Flush();
            Changed?.Invoke();
        }

        // For single-run goals this is the best result ever reached, not a sum of runs.
        public int GetProgress(QuestDefinition quest) =>
            IsCompleted(quest.Id) ? quest.Target : Math.Min(quest.Target, GetQuestProgress(quest.Id));

        public void RecordBest(QuestMetric metric, int value)
        {
            if (!_isRunActive || value <= 0)
                return;

            bool changed = value > GetMetric(metric);
            if (changed)
                _progress[metric] = value;
            // A new eligible quest still needs this run's result even when the lifetime best is higher.
            foreach (QuestDefinition quest in Definitions)
                if (quest.Metric == metric && _activeQuests.Contains(quest.Id))
                    changed |= SetQuestProgress(quest, value);
            if (changed)
                RecordChange();
        }

        public void AddProgress(QuestMetric metric, int amount)
        {
            if (!_isRunActive || amount <= 0)
                return;

            _progress[metric] = (int)Math.Min(int.MaxValue, (long)GetMetric(metric) + amount);
            foreach (QuestDefinition quest in Definitions)
                if (quest.Metric == metric && _activeQuests.Contains(quest.Id))
                    SetQuestProgress(quest, (int)Math.Min(int.MaxValue, (long)GetQuestProgress(quest.Id) + amount));
            RecordChange();
        }

        public void RecordAbilityLevel(AbilityName ability, int level)
        {
            if (!_isRunActive || level <= 0)
                return;

            bool changed = false;
            foreach (QuestDefinition quest in Definitions)
                if (quest.Metric == QuestMetric.SpecificAbilityLevel && quest.Ability != null &&
                    quest.Ability.AbilityName == ability && _activeQuests.Contains(quest.Id))
                    changed |= SetQuestProgress(quest, level);
            if (changed)
                RecordChange();
        }

        public void Tick()
        {
            if (!_dirty)
                return;

            _saveTimer += Time.unscaledDeltaTime;
            if (_saveTimer >= SaveInterval)
                Flush();
        }

        public void Flush()
        {
            if (!_dirty)
                return;

            var data = new QuestSaveData { Version = 4, Silver = _wallet.Silver.Count };
            foreach (var entry in _progress)
                data.Progress[entry.Key.ToString()] = entry.Value;
            foreach (var entry in _questProgress)
                data.QuestProgress[entry.Key] = entry.Value;
            data.CompletedIds.AddRange(_completed);
            data.PurchasedIds.AddRange(_purchased);
            data.ClaimedRewardIds.AddRange(_claimedRewards);
            data.ViewedUnlockIds.AddRange(_viewedUnlocks);

            PlayerPrefs.SetString(SaveKey, JsonConvert.SerializeObject(data));
            PlayerPrefs.Save();
            _dirty = false;
            _saveTimer = 0f;
        }

        public void Dispose()
        {
            Flush();
            _wallet.Silver.CountChanged -= HandleSilverChanged;
            Application.quitting -= Flush;
            Application.focusChanged -= HandleFocusChanged;
        }

        private bool IsDefaultContent(UnlockDefinition unlock)
        {
            switch (unlock.Category)
            {
                case ProgressionCategory.Characters:
                    foreach (string id in Configuration.DefaultCharacters)
                        if (id == unlock.CharacterId)
                            return true;
                    break;
                case ProgressionCategory.Weapons:
                case ProgressionCategory.Scrolls:
                    if (unlock.Ability != null)
                        foreach (AbilityName id in Configuration.DefaultAbilities)
                            if (id == unlock.Ability.AbilityName)
                                return true;
                    break;
                case ProgressionCategory.Relics:
                    if (unlock.Relic != null)
                        foreach (RelicDefinition relic in Configuration.DefaultRelics)
                            if (relic != null && relic.Id == unlock.Relic.Id)
                                return true;
                    break;
            }
            return false;
        }

        private int GetMetric(QuestMetric metric) =>
            _progress.TryGetValue(metric, out int value) ? value : 0;

        private int GetQuestProgress(string questId) =>
            _questProgress.TryGetValue(questId, out int value) ? value : 0;

        private bool SetQuestProgress(QuestDefinition quest, int value)
        {
            value = Math.Min(quest.Target, value);
            if (value <= GetQuestProgress(quest.Id) || IsCompleted(quest.Id))
                return false;
            _questProgress[quest.Id] = value;
            return true;
        }

        private bool ArePrerequisitesMet(QuestDefinition quest)
        {
            if (CompletedRuns < quest.MinimumCompletedRuns)
                return false;
            foreach (string questId in quest.RequiredQuestIds)
                if (!IsCompleted(questId))
                    return false;
            foreach (string unlockId in quest.RequiredUnlockIds)
                if (!IsOwned(GetUnlock(unlockId)))
                    return false;
            return true;
        }

        private void RecordChange()
        {
            _dirty = true;
            if (CompleteEligibleQuests())
                Flush();
            Changed?.Invoke();
        }

        private bool CompleteEligibleQuests()
        {
            bool completedAny = false;
            foreach (QuestDefinition quest in Definitions)
            {
                if (IsCompleted(quest.Id) || GetQuestProgress(quest.Id) < quest.Target || !ArePrerequisitesMet(quest))
                    continue;

                // Completion unlocks the content; silver is credited only when the reward is claimed.
                _completed.Add(quest.Id);
                _dirty = true;
                completedAny = true;
                QuestCompleted?.Invoke(quest);
            }

            return completedAny;
        }

        private void HandleSilverChanged(int amount)
        {
            _dirty = true;
            if (!_updatingWallet)
                Changed?.Invoke();
        }

        private void HandleFocusChanged(bool hasFocus)
        {
            if (!hasFocus)
                Flush();
        }

        private void RestoreLegacyQuestCompletions()
        {
            foreach (QuestDefinition quest in Definitions)
            {
                foreach (string legacyId in quest.LegacyQuestIds)
                {
                    if (!IsCompleted(legacyId))
                        continue;

                    // Preserve earned purchase eligibility and unclaimed rewards without replaying notifications.
                    _dirty |= _completed.Add(quest.Id);
                    if (IsRewardClaimed(legacyId))
                        _dirty |= _claimedRewards.Add(quest.Id);
                }
            }
        }

        private void Load()
        {
            if (!PlayerPrefs.HasKey(SaveKey))
                return;

            try
            {
                QuestSaveData data = JsonConvert.DeserializeObject<QuestSaveData>(PlayerPrefs.GetString(SaveKey));
                if (data == null)
                    return;

                if (data.Progress != null)
                    foreach (var entry in data.Progress)
                        if (Enum.TryParse(entry.Key, out QuestMetric metric) && Enum.IsDefined(typeof(QuestMetric), metric))
                            _progress[metric] = Math.Max(0, entry.Value);

                if (data.Version >= 4 && data.QuestProgress != null)
                {
                    foreach (var entry in data.QuestProgress)
                        if (GetQuest(entry.Key) != null)
                            _questProgress[entry.Key] = Math.Max(0, entry.Value);
                }
                else
                {
                    // Old global totals cannot satisfy steps that were never eligible in that save.
                    foreach (QuestDefinition quest in Definitions)
                        if (!quest.HasPrerequisites)
                            _questProgress[quest.Id] = Math.Min(quest.Target, GetMetric(quest.Metric));
                    _dirty = true;
                }

                if (data.CompletedIds != null)
                    foreach (string id in data.CompletedIds)
                        if (!string.IsNullOrWhiteSpace(id))
                            _completed.Add(id);

                if (data.PurchasedIds != null)
                    foreach (string id in data.PurchasedIds)
                        if (!string.IsNullOrWhiteSpace(id))
                            _purchased.Add(id);

                // Older saves already received their completed quests' silver automatically.
                if (data.Version < 3)
                {
                    _claimedRewards.UnionWith(_completed);
                    _dirty = true;
                }
                else if (data.ClaimedRewardIds != null)
                {
                    foreach (string id in data.ClaimedRewardIds)
                        if (IsCompleted(id))
                            _claimedRewards.Add(id);
                }

                if (data.ViewedUnlockIds != null)
                    foreach (string id in data.ViewedUnlockIds)
                        if (!string.IsNullOrWhiteSpace(id))
                            _viewedUnlocks.Add(id);

                _wallet.Silver.Set(Math.Max(0, data.Silver));
            }
            catch (JsonException exception)
            {
                Debug.LogWarning($"Could not read quest progress: {exception.Message}");
            }
        }

        [Serializable]
        private sealed class QuestSaveData
        {
            // Missing Version fields must retain the legacy automatic-reward behavior on migration.
            public int Version = 2;
            public Dictionary<string, int> Progress = new();
            public Dictionary<string, int> QuestProgress = new();
            public List<string> CompletedIds = new();
            public List<string> PurchasedIds = new();
            public List<string> ClaimedRewardIds = new();
            public List<string> ViewedUnlockIds = new();
            public int Silver;
        }
    }
}
