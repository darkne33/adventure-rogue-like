using System.Collections.Generic;
using Features.Quests.Scripts;

namespace Features.Relics.Scripts
{
    public sealed class RelicUnlockService
    {
        private readonly QuestService _questService;
        private readonly HashSet<string> _unlockedRelics = new();

        public RelicUnlockService(QuestService questService)
        {
            _questService = questService;
        }

        public bool IsUnlocked(RelicDefinition relic)
        {
            if (relic == null)
                return false;

            return _unlockedRelics.Contains(relic.Id) ||
                   _questService.IsRelicOwned(relic);
        }

        public void UnlockRelic(string relicId)
        {
            if (string.IsNullOrWhiteSpace(relicId) == false)
                _unlockedRelics.Add(relicId);
        }
    }
}
