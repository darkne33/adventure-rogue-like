using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.Enemies.Scripts.Level.Scripts;
using UI;
using UnityEngine;

namespace Features.Relics.Scripts
{
    internal sealed class RelicChestOpeningSequence
    {
        private const float MinimumOpeningDuration = 1f;
        private const float MinimumClaimHoldDuration = 0.3f;

        private readonly RelicChestOpeningView _view;
        private readonly RelicChestConfiguration _configuration;
        private readonly RelicEventBus _eventBus;
        private readonly IPanelService _panelService;
        private readonly IRoomTransitionService _roomTransitionService;
        private readonly RelicChestRewardPresenter _rewardPresenter;

        public RelicChestOpeningSequence(RelicChestOpeningView view,
            RelicChestConfiguration configuration, RelicEventBus eventBus,
            IPanelService panelService, IRoomTransitionService roomTransitionService,
            RelicChestRewardPresenter rewardPresenter)
        {
            _view = view;
            _configuration = configuration;
            _eventBus = eventBus;
            _panelService = panelService;
            _roomTransitionService = roomTransitionService;
            _rewardPresenter = rewardPresenter;
        }

        public async UniTask PlayAsync(CharacterFacade character, RelicDefinition relic,
            Vector3 chestPosition, Action onStarted, CancellationToken cancellationToken)
        {
            if (_roomTransitionService.IsPlaying)
                return;

            Transform relicRootTarget = character.RelicRootTarget;
            if (relicRootTarget == null)
            {
                Debug.LogError($"{character.name} is missing RelicRootTarget.", character);
                return;
            }

            bool characterLockAcquired = false;
            CharacterPanelVisibilityScope panelScope = null;
            CancellationTokenSource linkedTokenSource = null;
            CancellationToken sequenceToken = cancellationToken;
            GameObject rewardObject = null;
            UniTask openingDelay = default;
            bool openingStarted = false;

            try
            {
                if (character.TryBeginChestOpening() == false)
                    return;

                characterLockAcquired = true;
                onStarted?.Invoke();

                linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,
                    character.GetCancellationTokenOnDestroy());
                sequenceToken = linkedTokenSource.Token;
                panelScope = CharacterPanelVisibilityScope.Hide(_panelService);

                float openingDuration = Mathf.Max(MinimumOpeningDuration, _configuration.OpeningDuration);
                float preparationDuration = Mathf.Max(0f,
                    _configuration.ScreenFadePreparationDuration);

                await _roomTransitionService.Play(() =>
                {
                    sequenceToken.ThrowIfCancellationRequested();
                    character.PrepareChestOpening(_view.CharacterPosition);
                    _view.Begin();
                    character.StartChestOpeningAnimation();
                    _eventBus.PublishChestOpened(chestPosition);

                    openingDelay = UniTask.Delay(TimeSpan.FromSeconds(openingDuration),
                        cancellationToken: sequenceToken);
                    openingStarted = true;

                    return UniTask.CompletedTask;
                }, preparationDuration);

                if (openingStarted == false)
                    return;

                await openingDelay;

                character.EndChestOpeningAnimation();
                _view.ShowClaim();
                if (_rewardPresenter.TryPresent(relic, relicRootTarget, out rewardObject))
                    _view.StopTreasureRays();

                float claimHoldDuration = Mathf.Max(MinimumClaimHoldDuration,
                    _configuration.ClaimHoldDuration);
                await UniTask.Delay(TimeSpan.FromSeconds(claimHoldDuration),
                    cancellationToken: sequenceToken);
            }
            catch (OperationCanceledException) when (sequenceToken.IsCancellationRequested)
            {
            }
            finally
            {
                try
                {
                    try
                    {
                        _view.End();
                    }
                    finally
                    {
                        panelScope?.Dispose();
                    }
                }
                finally
                {
                    linkedTokenSource?.Dispose();

                    if (rewardObject != null)
                        UnityEngine.Object.Destroy(rewardObject);

                    bool ownsCharacterLock = characterLockAcquired ||
                                             (character != null && character.IsChestOpening);
                    if (ownsCharacterLock && character != null)
                        character.FinishChestOpening();
                }
            }
        }
    }
}
