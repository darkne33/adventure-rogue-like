using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Features.Relics.Scripts
{
    public sealed class RelicPool
    {
        private readonly RelicPoolConfiguration _configuration;
        private readonly RelicUnlockService _unlockService;

        public IReadOnlyList<RelicDefinition> AllRelics => _configuration.Relics;

        public RelicPool(RelicPoolConfiguration configuration, RelicUnlockService unlockService)
        {
            _configuration = configuration;
            _unlockService = unlockService;
        }

        public RelicDefinition GetById(string id) =>
            _configuration.Relics.FirstOrDefault(relic => relic != null && relic.Id == id);

        public RelicDefinition Roll(IReadOnlyCollection<RelicRuntimeState> activeRelics,
            IReadOnlyCollection<string> excludedIds = null)
        {
            List<RelicDefinition> candidates = GetAvailable(activeRelics, excludedIds).ToList();
            if (candidates.Count == 0)
                return null;

            int totalWeight = candidates.Sum(relic => Mathf.Max(0, _configuration.GetWeight(relic.Rarity)));
            if (totalWeight <= 0)
                return candidates[UnityEngine.Random.Range(0, candidates.Count)];

            int roll = UnityEngine.Random.Range(0, totalWeight);
            foreach (RelicDefinition relic in candidates)
            {
                roll -= Mathf.Max(0, _configuration.GetWeight(relic.Rarity));
                if (roll < 0)
                    return relic;
            }

            return candidates[^1];
        }

        public IEnumerable<RelicDefinition> GetAvailable(IReadOnlyCollection<RelicRuntimeState> activeRelics,
            IReadOnlyCollection<string> excludedIds = null)
        {
            HashSet<string> excluded = excludedIds != null
                ? new HashSet<string>(excludedIds)
                : new HashSet<string>();

            foreach (RelicDefinition relic in _configuration.Relics)
            {
                if (relic == null || excluded.Contains(relic.Id) || _unlockService.IsUnlocked(relic) == false)
                    continue;

                RelicRuntimeState owned = activeRelics?.FirstOrDefault(state => state.Definition == relic);
                if (owned == null)
                {
                    yield return relic;
                    continue;
                }

                if (relic.IsUnique || owned.StackCount >= relic.MaxStacks)
                    continue;

                yield return relic;
            }
        }
    }
}
