using System;
using System.Collections.Generic;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Features.Enemies.Scripts;
using Features.Relics.Scripts;
using UI;
using UnityEngine;

public class CharacterPanelPresenter : PanelPresenter<CharacterPanel>
{
    private readonly ICharacterLevelService _characterLevelService;
    private readonly IGameModeService _gameModeService;

    private Tween _expTween;
    private Tween _roomTween;
    private IRogueLikeRuntimeDataService _runtimeDataService;
    private MinimapController _minimapController;
    private RelicInventoryViewService _relicInventoryViewService;
    private CharacterWallet _characterWallet;
    private RelicEventBus _relicEventBus;
    private ICharacterAimTargetProvider _aimTargetProvider;
    private readonly HashSet<EnemyFacade> _countedKilledEnemies = new();
    private CancellationTokenSource _gameTimerCancellation;
    private CancellationTokenSource _crosshairCancellation;
    private int _killedEnemies;
    private int _shownRoomCount = -1;

    public CharacterPanelPresenter(ICharacterLevelService characterLevelService,
        IGameModeService gameModeService)
    {
        _characterLevelService = characterLevelService;
        _gameModeService = gameModeService;
    }

    public override UniTask Initialize()
    {
        Panel.WaveAlertText.DOFade(0, 0);
        Panel.RoomTimerView?.HideImmediate();
        _characterLevelService.OnUpdateAddExpView += UpdateExpView;
        _characterLevelService.OnExpAdded += ShowExpRewardView;
        RogueLikeStateMachine stateMachine = _gameModeService.Get<RogueLikeStateMachine>();
        _runtimeDataService = stateMachine.Resolve<IRogueLikeRuntimeDataService>() ??
                              throw new InvalidOperationException(
                                  "Rogue-like runtime data service is not available.");
        _minimapController = stateMachine.Resolve<MinimapController>();
        _relicInventoryViewService = stateMachine.Resolve<RelicInventoryViewService>();
        _characterWallet = stateMachine.Resolve<CharacterWallet>();
        _relicEventBus = stateMachine.Resolve<RelicEventBus>();
        _aimTargetProvider = stateMachine.Resolve<ICharacterAimTargetProvider>();

        _runtimeDataService.RoomChanged += HandleRoomChanged;
        _minimapController.Attach(Panel.MinimapView);
        _relicInventoryViewService.Attach(Panel);
        if (_characterWallet != null)
        {
            _characterWallet.Gold.CountChanged += UpdateGoldCurrencyView;
            _characterWallet.Silver.CountChanged += UpdateSilverCurrencyView;
        }

        if (_relicEventBus != null)
            _relicEventBus.Kill += UpdateKilledEnemiesView;

        UpdateExpView(_characterLevelService.GetCurrentExp, _characterLevelService.GetMaxExp);
        UpdateRoomView();
        UpdateGoldCurrencyView(_characterWallet?.Gold.Count ?? 0);
        UpdateSilverCurrencyView(_characterWallet?.Silver.Count ?? 0);
        ResetKilledEnemiesView();
        StartGameTimer();
        StartCrosshairTracking();

        return UniTask.CompletedTask;
    }

    public override UniTask OnClosed()
    {
        _characterLevelService.OnUpdateAddExpView -= UpdateExpView;
        _characterLevelService.OnExpAdded -= ShowExpRewardView;
        if (_runtimeDataService != null)
            _runtimeDataService.RoomChanged -= HandleRoomChanged;

        if (_characterWallet != null)
        {
            _characterWallet.Gold.CountChanged -= UpdateGoldCurrencyView;
            _characterWallet.Silver.CountChanged -= UpdateSilverCurrencyView;
        }

        if (_relicEventBus != null)
            _relicEventBus.Kill -= UpdateKilledEnemiesView;

        StopGameTimer();
        StopCrosshairTracking();
        _minimapController?.Detach(Panel.MinimapView);
        _relicInventoryViewService?.Detach();
        Panel.RoomTimerView?.HideImmediate();
        _expTween?.Kill();
        _roomTween?.Kill();
        return base.OnClosed();
    }

    private void UpdateExpView(int currentExp, int maxExp)
    {
        float value = (float)currentExp / maxExp;
        const float duration = 0.3f;

        _expTween?.Kill();
        _expTween = Panel.ExpProgressBar.DOValue(value, duration);
    }

    private void ShowExpRewardView(int amount) =>
        Panel.CharacterExpView.ShowExp(amount);

    private void HandleRoomChanged(RoomData previousRoom, RoomData currentRoom) =>
        UpdateRoomView(animate: true);

    private void UpdateRoomView(bool animate = false)
    {
        int roomCount = _runtimeDataService.VisitedRoomsCount;
        bool hasChanged = roomCount != _shownRoomCount;
        _shownRoomCount = roomCount;
        Panel.RoomNumberText.text = $"ROOM {roomCount}";

        if (!animate || !hasChanged)
            return;

        RectTransform roomTextTransform = Panel.RoomNumberText.rectTransform;
        roomTextTransform.localScale = Vector3.one;
        _roomTween?.Kill();
        _roomTween = roomTextTransform
            .DOPunchScale(Vector3.one * 0.15f, 0.35f, 6, 0.5f)
            .SetUpdate(true);
    }

    private void UpdateGoldCurrencyView(int amount) =>
        SetText(Panel.PlayerGoldCurrencyText, amount.ToString());

    private void UpdateSilverCurrencyView(int amount) =>
        SetText(Panel.PlayerSilverCurrencyText, amount.ToString());

    private void ResetKilledEnemiesView()
    {
        _killedEnemies = 0;
        _countedKilledEnemies.Clear();
        SetText(Panel.PlayerKilledEnemiesText, _killedEnemies.ToString());
    }

    private void UpdateKilledEnemiesView(RelicKillEvent killEvent)
    {
        if (killEvent.Target == null || _countedKilledEnemies.Add(killEvent.Target) == false)
            return;

        _killedEnemies++;
        SetText(Panel.PlayerKilledEnemiesText, _killedEnemies.ToString());
    }

    private void StartGameTimer()
    {
        StopGameTimer();
        _gameTimerCancellation = new CancellationTokenSource();
        UpdateGameTimerView(0);
        RunGameTimer(_gameTimerCancellation.Token).Forget();
    }

    private void StopGameTimer()
    {
        if (_gameTimerCancellation == null)
            return;

        _gameTimerCancellation.Cancel();
        _gameTimerCancellation.Dispose();
        _gameTimerCancellation = null;
    }

    private void StartCrosshairTracking()
    {
        StopCrosshairTracking();
        Panel.CrosshairView?.SetTargeted(false);
        _crosshairCancellation = new CancellationTokenSource();
        RunCrosshairTracking(_crosshairCancellation.Token).Forget();
    }

    private void StopCrosshairTracking()
    {
        if (_crosshairCancellation == null)
            return;

        _crosshairCancellation.Cancel();
        _crosshairCancellation.Dispose();
        _crosshairCancellation = null;
        Panel.CrosshairView?.SetTargeted(false);
    }

    private async UniTask RunGameTimer(CancellationToken cancellationToken)
    {
        float elapsedTime = 0f;
        int lastShownSeconds = 0;

        try
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                elapsedTime += Time.deltaTime;
                int seconds = Mathf.FloorToInt(elapsedTime);
                if (seconds == lastShownSeconds)
                    continue;

                lastShownSeconds = seconds;
                UpdateGameTimerView(seconds);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async UniTask RunCrosshairTracking(CancellationToken cancellationToken)
    {
        try
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, cancellationToken);
                Panel.CrosshairView?.SetTargeted(_aimTargetProvider?.GetAimedEnemy() != null);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private void UpdateGameTimerView(int seconds) =>
        SetText(Panel.GameTimerText, FormatTime(seconds));

    private static void SetText(TMPro.TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }

    private static string FormatTime(int seconds)
    {
        seconds = Mathf.Max(0, seconds);
        int minutes = seconds / 60;
        int remainingSeconds = seconds % 60;
        return $"{minutes:00}:{remainingSeconds:00}";
    }
}
