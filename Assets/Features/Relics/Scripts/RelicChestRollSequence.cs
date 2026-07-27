using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Relics.Scripts
{
    internal sealed class RelicChestRollSequence
    {
        private const int RarityStageCount = 4;
        private const float MinimumStageDuration = 0.1f;
        private const float MinimumPreviewInterval = 0.02f;

        private readonly RelicChestRollView _view;
        private readonly RelicChestConfiguration _configuration;
        private readonly RelicEventBus _eventBus;
        private readonly RelicChestRewardPresenter _rewardPresenter;

        public RelicChestRollSequence(RelicChestRollView view,
            RelicChestConfiguration configuration, RelicEventBus eventBus,
            RelicChestRewardPresenter rewardPresenter)
        {
            _view = view;
            _configuration = configuration;
            _eventBus = eventBus;
            _rewardPresenter = rewardPresenter;
        }

        public async UniTask PlayAsync(IReadOnlyList<RelicDefinition> availableRelics,
            Vector3 chestPosition,
            Action onFinished, CancellationToken cancellationToken)
        {
            RelicPickup preview = null;

            try
            {
                if (TryGetInitialStage(availableRelics, out RelicRarity rarity,
                        out List<RelicDefinition> stageCandidates) == false)
                {
                    Debug.LogError("Relic chest has no available rewards.");
                    return;
                }

                RelicDefinition displayedRelic = GetNextPreview(stageCandidates, null,
                    stageCandidates[0]);
                if (_rewardPresenter.TryCreatePreview(displayedRelic, _view.RewardRoot,
                        out preview) == false)
                    return;

                _eventBus.PublishChestOpened(chestPosition);

                float stageDuration = Mathf.Max(MinimumStageDuration,
                    _configuration.RarityStageDuration);
                float maximumRollDuration = stageDuration * RarityStageCount +
                                            Mathf.Max(0f,
                                                _configuration.RarityUpgradeTransitionDuration) *
                                            (RarityStageCount - 1);

                _view.SetRarity(rarity);
                _view.Begin(maximumRollDuration, _configuration.ChestShakePositionStrength,
                    _configuration.ChestShakeRotationStrength, _configuration.ChestShakeVibrato);

                while (true)
                {
                    displayedRelic = await PlayPreviewRoll(preview, stageCandidates,
                        displayedRelic, stageDuration, cancellationToken);

                    if (TryUpgradeRarity(rarity, availableRelics, out RelicRarity upgradedRarity,
                            out List<RelicDefinition> upgradedCandidates) == false)
                        break;

                    displayedRelic = GetNextPreview(upgradedCandidates, null,
                        upgradedCandidates[0]);
                    _rewardPresenter.UpdatePreview(preview, displayedRelic);
                    _view.UpgradeRarity(upgradedRarity,
                        _configuration.RarityUpgradePumpDuration,
                        _configuration.RarityUpgradePumpStrength);

                    float transitionDuration = Mathf.Max(0f,
                        _configuration.RarityUpgradeTransitionDuration);
                    await UniTask.Delay(TimeSpan.FromSeconds(transitionDuration),
                        cancellationToken: cancellationToken);

                    rarity = upgradedRarity;
                    stageCandidates = upgradedCandidates;
                }

                RelicDefinition reward =
                    stageCandidates[UnityEngine.Random.Range(0, stageCandidates.Count)];
                _rewardPresenter.UpdatePreview(preview, reward);
                _view.Reveal(preview.transform);

                float revealDuration = Mathf.Max(0f, _configuration.FinalRevealDuration);
                await UniTask.Delay(TimeSpan.FromSeconds(revealDuration),
                    cancellationToken: cancellationToken);

                bool granted = await _rewardPresenter.GrantAndDismissAsync(preview, reward);
                if (granted)
                    preview = null;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            finally
            {
                _view.End();

                if (preview != null)
                    UnityEngine.Object.Destroy(preview.gameObject);

                onFinished?.Invoke();
            }
        }

        private async UniTask<RelicDefinition> PlayPreviewRoll(RelicPickup preview,
            IReadOnlyList<RelicDefinition> candidates, RelicDefinition current, float duration,
            CancellationToken cancellationToken)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                current = GetNextPreview(candidates, current, candidates[0]);
                _rewardPresenter.UpdatePreview(preview, current);
                _view.PulsePreview(preview.transform);

                float progress = Mathf.Clamp01(elapsed / duration);
                float startInterval = Mathf.Max(MinimumPreviewInterval,
                    _configuration.PreviewStartInterval);
                float endInterval = Mathf.Max(startInterval, _configuration.PreviewEndInterval);
                float interval = Mathf.Lerp(startInterval, endInterval, progress * progress);
                float delay = Mathf.Min(interval, duration - elapsed);

                await UniTask.Delay(TimeSpan.FromSeconds(delay),
                    cancellationToken: cancellationToken);
                elapsed += delay;
            }

            return current;
        }

        private bool TryUpgradeRarity(RelicRarity currentRarity,
            IReadOnlyList<RelicDefinition> availableRelics, out RelicRarity upgradedRarity,
            out List<RelicDefinition> upgradedCandidates)
        {
            upgradedCandidates = null;
            if (TryGetNextRarity(currentRarity, out upgradedRarity) == false)
                return false;

            if (TryGetCandidates(availableRelics, upgradedRarity, out upgradedCandidates) == false)
                return false;

            float upgradeChance = _configuration.GetRarityUpgradeChance(currentRarity);
            return upgradeChance > 0f && UnityEngine.Random.value < upgradeChance;
        }

        private static bool TryGetInitialStage(IReadOnlyList<RelicDefinition> availableRelics,
            out RelicRarity rarity, out List<RelicDefinition> candidates)
        {
            for (int rarityIndex = (int)RelicRarity.Common;
                 rarityIndex <= (int)RelicRarity.Legendary;
                 rarityIndex++)
            {
                rarity = (RelicRarity)rarityIndex;
                if (TryGetCandidates(availableRelics, rarity, out candidates))
                    return true;
            }

            rarity = RelicRarity.Common;
            candidates = null;
            return false;
        }

        private static bool TryGetCandidates(IReadOnlyList<RelicDefinition> availableRelics,
            RelicRarity rarity, out List<RelicDefinition> candidates)
        {
            candidates = new List<RelicDefinition>();
            if (availableRelics == null)
                return false;

            foreach (RelicDefinition relic in availableRelics)
            {
                if (relic != null && relic.Rarity == rarity)
                    candidates.Add(relic);
            }

            return candidates.Count > 0;
        }

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

        private static RelicDefinition GetNextPreview(IReadOnlyList<RelicDefinition> candidates,
            RelicDefinition previous, RelicDefinition fallback)
        {
            if (candidates == null || candidates.Count == 0)
                return fallback;

            for (int attempt = 0; attempt < candidates.Count * 2; attempt++)
            {
                RelicDefinition candidate = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                if (candidate != null && candidate != previous)
                    return candidate;
            }

            return fallback;
        }
    }
}
