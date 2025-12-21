using System.Threading;
using Cysharp.Threading.Tasks;

namespace CustomPackages.Package.StateMachine.States
{
    public interface IState : IExitableState
    {
        public UniTask Enter(CancellationToken cts = default);
    }
}