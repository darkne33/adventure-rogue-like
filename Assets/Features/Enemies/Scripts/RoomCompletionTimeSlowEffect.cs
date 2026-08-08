using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    [Serializable]
    public sealed class RoomCompletionTimeSlowSettings
    {
        [SerializeField, Range(0.01f, 1f)] private float _timeScale = 0.15f;
        [SerializeField, Min(0f)] private float _holdDuration = 0.32f;
        [SerializeField, Min(0f)] private float _recoveryDuration = 0.56f;

        public float TimeScale => Mathf.Clamp(_timeScale, 0.01f, 1f);
        public float HoldDuration => Mathf.Max(0f, _holdDuration);
        public float RecoveryDuration => Mathf.Max(0f, _recoveryDuration);
    }

    public sealed class RoomCompletionTimeSlowEffect : IDisposable
    {
        private readonly EnemyRoomObserver _enemyRoomObserver;
        private readonly RoomCompletionTimeSlowSettings _settings;
        private readonly ITimeScaleService _timeScaleService;

        private bool _isPlaying;
        private bool _isDisposed;
        private ITimeScaleRequest _timeScaleRequest;

        public RoomCompletionTimeSlowEffect(EnemyRoomObserver enemyRoomObserver,
            RoomCompletionTimeSlowSettings settings, ITimeScaleService timeScaleService)
        {
            _enemyRoomObserver = enemyRoomObserver;
            _settings = settings;
            _timeScaleService = timeScaleService;

            _enemyRoomObserver.RoomCompleted += HandleRoomCompleted;
        }

        public void Dispose()
        {
            _enemyRoomObserver.RoomCompleted -= HandleRoomCompleted;
            _isDisposed = true;
            _timeScaleRequest?.Dispose();
            _timeScaleRequest = null;
        }

        private void HandleRoomCompleted(DefaultEnemiesRoomData _) =>
            PlayAsync().Forget();

        private async UniTask PlayAsync()
        {
            if (_isPlaying || _isDisposed)
                return;

            _isPlaying = true;
            ITimeScaleRequest timeScaleRequest = _timeScaleService.Request(_settings.TimeScale);
            _timeScaleRequest = timeScaleRequest;

            try
            {
                if (!await WaitForUnscaledDuration(_settings.HoldDuration, timeScaleRequest))
                    return;

                await RecoverTimeScale(timeScaleRequest);
            }
            finally
            {
                timeScaleRequest.Dispose();
                if (ReferenceEquals(_timeScaleRequest, timeScaleRequest))
                    _timeScaleRequest = null;

                _isPlaying = false;
            }
        }

        private async UniTask RecoverTimeScale(ITimeScaleRequest timeScaleRequest)
        {
            float recoveryDuration = _settings.RecoveryDuration;
            if (recoveryDuration <= 0f)
                return;

            float elapsed = 0f;

            while (elapsed < recoveryDuration)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);

                if (!CanContinue(timeScaleRequest))
                    return;

                if (_timeScaleService.IsPaused)
                    continue;

                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / recoveryDuration);
                float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
                timeScaleRequest.TimeScale = Mathf.Lerp(_settings.TimeScale, 1f, easedProgress);
            }
        }

        private async UniTask<bool> WaitForUnscaledDuration(float duration,
            ITimeScaleRequest timeScaleRequest)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);

                if (!CanContinue(timeScaleRequest))
                    return false;

                if (_timeScaleService.IsPaused)
                    continue;

                elapsed += Time.unscaledDeltaTime;
            }

            return true;
        }

        private bool CanContinue(ITimeScaleRequest timeScaleRequest) =>
            !_isDisposed && ReferenceEquals(_timeScaleRequest, timeScaleRequest);
    }
}
