using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CustomPackages.Package.Extensions.Solo.Services
{
    public class SoloThread : ISoloThread
    {
        public event Action BecameIdle;

        public bool IsBusy() => m_isBusy;

        public async UniTask<IDisposable> ScheduleSolo()
        {
            if (IsBusy())
            {
                UniTaskCompletionSource source = new ();
                m_queue.Enqueue(source);

                await source.Task;
            }

            return new SoloControl(this);
        }
        
        public void ScheduleSolo(SoloAction action, CancellationToken cancellationToken)
        {
            Background().Forget();
            
            async UniTaskVoid Background()
            {
                using (await ScheduleSolo())
                {
                    await action.Invoke(cancellationToken);
                }
            }
        }
        
        public bool TrySolo(SoloAction soloAction, CancellationToken cancellationToken)
        {
            if (IsBusy()) 
                return false;
            
            Background().Forget();
            return true;
            
            async UniTaskVoid Background()
            {
                using (await ScheduleSolo())
                {
                    await soloAction.Invoke(cancellationToken);
                }
            }
        }
        
        public bool TrySolo(SoloAction soloAction)
        {
            if (IsBusy()) 
                return false;
            
            Background().Forget();
            return true;
            
            async UniTaskVoid Background()
            {
                using (await ScheduleSolo())
                {
                    await soloAction.Invoke(GetToken());
                }
            }
        }

        public void SetBusy()
        {
            m_isBusy = true;
        }

        public void SetPending()
        {
            bool hasAwaitingTask = m_queue.Count > 0;

            if (hasAwaitingTask)
            {
                m_queue.Dequeue().TrySetResult();
            }
            else
            {
                m_isBusy = false;
                BecameIdle?.Invoke();
            }
        }

        public void BreakSchedule()
        {
            m_queue.Clear();
            Dispose();
        }

        public void Dispose()
        {
            m_cancellationTokenSource?.Cancel();
            m_cancellationTokenSource?.Dispose();
            m_cancellationTokenSource = null;
        }
        
        private CancellationToken GetToken()
        {
            m_cancellationTokenSource ??= new CancellationTokenSource();
            return m_cancellationTokenSource.Token;
        }
        
        private Queue<UniTaskCompletionSource> m_queue = new();
        private CancellationTokenSource m_cancellationTokenSource = new();
        private bool m_isBusy;
    }
}