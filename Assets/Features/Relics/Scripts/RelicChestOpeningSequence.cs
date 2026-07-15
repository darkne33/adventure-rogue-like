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
        private readonly CharacterChestOpeningService _chestOpeningService;
        private readonly RelicChestRewardPresenter _rewardPresenter;

        public RelicChestOpeningSequence(RelicChestOpeningView view,
            RelicChestConfiguration configuration, RelicEventBus eventBus,
            IPanelService panelService, IRoomTransitionService roomTransitionService,
            CharacterChestOpeningService chestOpeningService,
            RelicChestRewardPresenter rewardPresenter)
        {
            _view = view;
            _configuration = configuration;
            _eventBus = eventBus;
            _panelService = panelService;
            _roomTransitionService = roomTransitionService;
            _chestOpeningService = chestOpeningService;
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
                if (_chestOpeningService.TryBegin() == false)
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

                await _roomTransitionService.PlaySolidFade(() =>
                {
                    sequenceToken.ThrowIfCancellationRequested();
                    _chestOpeningService.Prepare(_view.CharacterPosition);
                    _view.Begin();
                    _chestOpeningService.StartAnimation();
                    _eventBus.PublishChestOpened(chestPosition);

                    openingDelay = UniTask.Delay(TimeSpan.FromSeconds(openingDuration),
                        cancellationToken: sequenceToken);
                    openingStarted = true;

                    return UniTask.CompletedTask;
                }, preparationDuration);

                if (openingStarted == false)
                    return;

                await openingDelay;

                _chestOpeningService.FinishAnimation();
                float finishAnimationDuration = Mathf.Max(0f,
                    _configuration.FinishAnimationDuration);
                await UniTask.Delay(TimeSpan.FromSeconds(finishAnimationDuration),
                    cancellationToken: sequenceToken);

                _view.BeginClaimCamera();
                float cameraClaimDuration = Mathf.Max(0f, _configuration.CameraClaimDuration);
                await UniTask.Delay(TimeSpan.FromSeconds(cameraClaimDuration),
                    cancellationToken: sequenceToken);

                _chestOpeningService.EndAnimation();
                float endAnimationDuration = Mathf.Max(0f,
                    _configuration.EndAnimationDuration);
                await UniTask.Delay(TimeSpan.FromSeconds(endAnimationDuration),
                    cancellationToken: sequenceToken);

                _view.PlayTreasureOpen();
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

                    if (characterLockAcquired)
                        _chestOpeningService.Finish();
                }
            }
        }
    }
}
