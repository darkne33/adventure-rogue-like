using System.Threading;
using Cysharp.Threading.Tasks;

namespace CustomPackages.Package.StateMachine.States
{
    public interface IExitableState
    {
        public UniTask Exit(CancellationToken cts = default);
    }
}