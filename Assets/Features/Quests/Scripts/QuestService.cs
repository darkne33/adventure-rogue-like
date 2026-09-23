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
        private readonly HashSet<string> _completed = new();
        private readonly HashSet<string> _purchased = new();
        private bool _dirty;
        private bool _updatingWallet;
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
            _wallet.Silver.CountChanged += HandleSilverChanged;
            Application.quitting += Flush;
            Application.focusChanged += HandleFocusChanged;
            // Stable metric totals remain useful when the editable quest catalog changes.
            foreach (QuestMetric metric in Enum.GetValues(typeof(QuestMetric)))
                CompleteEligibleQuests(metric);
            Flush();
        }

        public bool IsCompleted(string questId) =>
            !string.IsNullOrWhiteSpace(questId) && _completed.Contains(questId);

        public QuestDefinition GetQuest(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId))
                return null;
            foreach (QuestDefinition quest in Definitions)
                if (quest.Id == questId)
                    return quest;
            return null;
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
            IsCompleted(quest.Id) ? quest.Target : Math.Min(quest.Target, GetMetric(quest.Metric));

        public void RecordBest(QuestMetric metric, int value)
        {
            if (value <= GetMetric(metric))
                return;

            _progress[metric] = value;
            RecordChange(metric);
        }

        public void AddProgress(QuestMetric metric, int amount)
        {
            if (amount <= 0)
                return;

            _progress[metric] = (int)Math.Min(int.MaxValue, (long)GetMetric(metric) + amount);
            RecordChange(metric);
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

            var data = new QuestSaveData { Silver = _wallet.Silver.Count };
            foreach (var entry in _progress)
                data.Progress[entry.Key.ToString()] = entry.Value;
            data.CompletedIds.AddRange(_completed);
            data.PurchasedIds.AddRange(_purchased);

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

        private void RecordChange(QuestMetric metric)
        {
            _dirty = true;
            if (CompleteEligibleQuests(metric))
                Flush();
            Changed?.Invoke();
        }

        private bool CompleteEligibleQuests(QuestMetric metric)
        {
            bool completedAny = false;
            foreach (QuestDefinition quest in Definitions)
            {
                if (quest.Metric != metric || IsCompleted(quest.Id) || GetMetric(metric) < quest.Target)
                    continue;

                // Completion and reward are saved together, so reopening the menu cannot grant it again.
                _completed.Add(quest.Id);
                _dirty = true;
                _updatingWallet = true;
                try
                {
                    _wallet.Silver.Set((int)Math.Min(int.MaxValue, (long)Silver + quest.SilverReward));
                }
                finally
                {
                    _updatingWallet = false;
                }
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

                if (data.CompletedIds != null)
                    foreach (string id in data.CompletedIds)
                        if (!string.IsNullOrWhiteSpace(id))
                            _completed.Add(id);

                if (data.PurchasedIds != null)
                    foreach (string id in data.PurchasedIds)
                        if (!string.IsNullOrWhiteSpace(id))
                            _purchased.Add(id);

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
            public int Version = 2;
            public Dictionary<string, int> Progress = new();
            public List<string> CompletedIds = new();
            public List<string> PurchasedIds = new();
            public int Silver;
        }
    }
}
