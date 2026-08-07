using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Features.Leaderboard
{
    public sealed class RoomLeaderboardReporter : IInitializable, IDisposable
    {
        private readonly IRogueLikeRuntimeDataService _runtimeDataService;
        private readonly ILeaderboardService _leaderboardService;
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        private int _latestVisitedRooms;
        private int _lastReportedRoom;
        private bool _isReporting;

        public RoomLeaderboardReporter(IRogueLikeRuntimeDataService runtimeDataService,
            ILeaderboardService leaderboardService)
        {
            _runtimeDataService = runtimeDataService;
            _leaderboardService = leaderboardService;
        }

        public void Initialize() =>
            _runtimeDataService.RoomChanged += HandleRoomChanged;

        public void BeginRun()
        {
            _latestVisitedRooms = 0;
            _lastReportedRoom = 0;
            BeginRunOnServer(_cancellationTokenSource.Token).Forget();
        }

        public void Dispose()
        {
            _runtimeDataService.RoomChanged -= HandleRoomChanged;
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

        private void HandleRoomChanged(RoomData previousRoom, RoomData currentRoom)
        {
            int visitedRooms = _runtimeDataService.VisitedRoomsCount;
            if (_leaderboardService.IsConfigured == false ||
                visitedRooms <= _latestVisitedRooms)
                return;

            _latestVisitedRooms = visitedRooms;
            if (_isReporting == false)
                ReportPendingRooms(_cancellationTokenSource.Token).Forget();
        }

        private async UniTask BeginRunOnServer(CancellationToken cancellationToken)
        {
            if (_leaderboardService.IsConfigured == false)
                return;

            try
            {
                await _leaderboardService.StartRun(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to start PlayFab leaderboard run: {exception.Message}");
            }
        }

        private async UniTask ReportPendingRooms(CancellationToken cancellationToken)
        {
            _isReporting = true;
            try
            {
                while (_lastReportedRoom < _latestVisitedRooms)
                {
                    int latestRoomsAtAttemptStart = _latestVisitedRooms;
                    int roomSequence = _lastReportedRoom + 1;
                    if (await TryReportRoom(roomSequence, cancellationToken) == false)
                    {
                        if (_latestVisitedRooms > latestRoomsAtAttemptStart)
                            continue;

                        return;
                    }

                    _lastReportedRoom = roomSequence;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            finally
            {
                _isReporting = false;
            }
        }

        private async UniTask<bool> TryReportRoom(int roomSequence,
            CancellationToken cancellationToken)
        {
            const int maxAttempts = 3;
            Exception lastException = null;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    await _leaderboardService.ReportRoomVisited(roomSequence, cancellationToken);
                    return true;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    lastException = exception;
                    if (attempt < maxAttempts - 1)
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(1 << attempt),
                            ignoreTimeScale: true, cancellationToken: cancellationToken);
                    }
                }
            }

            Debug.LogWarning(
                $"Failed to report a visited room to PlayFab: {lastException?.Message}");
            return false;
        }
    }
}
