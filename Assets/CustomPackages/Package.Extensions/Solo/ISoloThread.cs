using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CustomPackages.Package.Extensions.Solo
{
    public interface ISoloThread : IDisposable
    {
        public event Action BecameIdle;
        public bool IsBusy();
        public UniTask<IDisposable> ScheduleSolo ();
        public void ScheduleSolo (SoloAction soloAction, CancellationToken cancellationToken);
        
        public bool TrySolo(SoloAction action, CancellationToken cancellationToken);
        public bool TrySolo(SoloAction action);
        public void BreakSchedule();
    }
    
    public delegate UniTask SoloAction(CancellationToken cancellationToken);
}