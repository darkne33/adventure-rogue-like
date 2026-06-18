using System.Collections.Generic;

namespace Features.Relics.Scripts
{
    public sealed class RelicUnlockService
    {
        private readonly HashSet<string> _completedQuests = new();
        private readonly HashSet<string> _unlockedRelics = new();

        public bool IsUnlocked(RelicDefinition relic)
        {
            if (relic == null)
                return false;

            if (relic.IsLockedByQuest == false)
                return true;

            return _unlockedRelics.Contains(relic.Id) ||
                   _completedQuests.Contains(relic.UnlockQuestId);
        }

        public void CompleteQuest(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId) == false)
                _completedQuests.Add(questId);
        }

        public void UnlockRelic(string relicId)
        {
            if (string.IsNullOrWhiteSpace(relicId) == false)
                _unlockedRelics.Add(relicId);
        }
    }
}
