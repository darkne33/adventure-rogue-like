using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CustomPackages.Package.Extensions
{
    public class DateTimeCounter : IDisposable
    {
        public event Action OnTimerCompleted;
        public NotifiedProperty<TimeSpan> Time { get; }

        private readonly CancellationTokenSource _cts;
        private readonly DateTime _startDate;
        private DateTime _endDate;

        public DateTimeCounter(DateTime startDate, DateTime endDate, CancellationTokenSource cts)
        {
            _startDate = startDate;
            _endDate = endDate;
            Time = new NotifiedProperty<TimeSpan>();
            _cts = cts;
            Timer().Forget();
        }

        private async UniTaskVoid Timer()
        {
            DateTime currentTime;
            UpdateTimer(_endDate);
            do
            {
                await UniTask.Delay(TimeSpan.FromSeconds(.5f), cancellationToken: _cts.Token);
                currentTime = UpdateTimer(_endDate);
            } while (currentTime >= _startDate && currentTime <= _endDate);

            OnTimerCompleted?.Invoke();
        }

        private DateTime UpdateTimer(DateTime endDate)
        {
            var currentTime = DateTime.Now;
            Time.Value = endDate - currentTime;
            return currentTime;
        }

        public void Cancel() =>
            _cts?.Cancel();

        public void Dispose() =>
            Cancel();

        public void Add(TimeSpan timeSpan) =>
            _endDate = _endDate.Add(timeSpan);

        public void Set(DateTime dateTime) =>
            _endDate = dateTime;

        public void Restart() => 
            Timer().Forget();
    }
}