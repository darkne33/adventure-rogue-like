using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Features.Relics.Scripts
{
    internal sealed class RelicChestRollPlan
    {
        public IReadOnlyList<RelicDefinition> AvailableRelics { get; }
        public RelicDefinition Reward { get; }

        public RelicChestRollPlan(IReadOnlyList<RelicDefinition> availableRelics,
            RelicDefinition reward)
        {
            AvailableRelics = availableRelics;
            Reward = reward;
        }
    }

    public sealed class RelicChestRollService
    {
        private readonly Dictionary<RelicDefinition, int> _reservedRewards = new();
        private int _lastInteractionFrame = -1;

        internal bool TryBegin(IReadOnlyList<RelicDefinition> availableRelics,
            IReadOnlyCollection<RelicRuntimeState> activeRelics,
            RelicChestConfiguration configuration, out RelicChestRollPlan rollPlan)
        {
            rollPlan = null;

            if (_lastInteractionFrame == Time.frameCount || configuration == null)
                return false;

            List<RelicDefinition> reservableRelics = availableRelics?
                .Where(relic => CanReserve(relic, activeRelics))
                .ToList();
            if (reservableRelics == null || reservableRelics.Count == 0 ||
                TryRollReward(reservableRelics, configuration, out RelicDefinition reward) == false)
                return false;

            _reservedRewards.TryGetValue(reward, out int reservedCount);
            _reservedRewards[reward] = reservedCount + 1;
            _lastInteractionFrame = Time.frameCount;
            rollPlan = new RelicChestRollPlan(reservableRelics, reward);
            return true;
        }

        internal void Finish(RelicChestRollPlan rollPlan)
        {
            RelicDefinition reward = rollPlan?.Reward;
            if (reward == null || _reservedRewards.TryGetValue(reward, out int reservedCount) == false)
                return;

            if (reservedCount <= 1)
                _reservedRewards.Remove(reward);
            else
                _reservedRewards[reward] = reservedCount - 1;
        }

        private bool CanReserve(RelicDefinition relic,
            IReadOnlyCollection<RelicRuntimeState> activeRelics)
        {
            if (relic == null)
                return false;

            int ownedCount = activeRelics?
                .FirstOrDefault(state => state.Definition == relic)?.StackCount ?? 0;
            _reservedRewards.TryGetValue(relic, out int reservedCount);
            int maxStacks = relic.IsUnique ? 1 : Mathf.Max(1, relic.MaxStacks);
            return ownedCount + reservedCount < maxStacks;
        }

        private static bool TryRollReward(IReadOnlyList<RelicDefinition> availableRelics,
            RelicChestConfiguration configuration, out RelicDefinition reward)
        {
            reward = null;
            if (TryGetInitialRarity(availableRelics, out RelicRarity rarity) == false)
                return false;

            while (TryGetNextRarity(rarity, out RelicRarity nextRarity) &&
                   HasRarity(availableRelics, nextRarity))
            {
                float upgradeChance = configuration.GetRarityUpgradeChance(rarity);
                if (upgradeChance <= 0f || UnityEngine.Random.value >= upgradeChance)
                    break;

                rarity = nextRarity;
            }

            List<RelicDefinition> candidates = availableRelics
                .Where(relic => relic != null && relic.Rarity == rarity)
                .ToList();
            if (candidates.Count == 0)
                return false;

            reward = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            return true;
        }

        private static bool TryGetInitialRarity(IReadOnlyList<RelicDefinition> availableRelics,
            out RelicRarity rarity)
        {
            for (int rarityIndex = (int)RelicRarity.Common;
                 rarityIndex <= (int)RelicRarity.Legendary;
                 rarityIndex++)
            {
                rarity = (RelicRarity)rarityIndex;
                if (HasRarity(availableRelics, rarity))
                    return true;
            }

            rarity = default;
            return false;
        }

        private static bool HasRarity(IReadOnlyList<RelicDefinition> availableRelics,
            RelicRarity rarity) =>
            availableRelics.Any(relic => relic != null && relic.Rarity == rarity);

        private static bool TryGetNextRarity(RelicRarity rarity, out RelicRarity nextRarity)
        {
            switch (rarity)
            {
                case RelicRarity.Common:
                    nextRarity = RelicRarity.Uncommon;
                    return true;
                case RelicRarity.Uncommon:
                    nextRarity = RelicRarity.Rare;
                    return true;
                case RelicRarity.Rare:
                    nextRarity = RelicRarity.Legendary;
                    return true;
                default:
                    nextRarity = default;
                    return false;
            }
        }
    }
}
