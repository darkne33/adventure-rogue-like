using System.Threading;
using Cysharp.Threading.Tasks;

namespace CustomPackages.Package.StateMachine.States
{
    public abstract class State : IState, IHavingStateMachine
    {
        protected StateMachine StateMachine { get; private set; }

        public void SetStateMachine(StateMachine stateMachine)
        {
            StateMachine = stateMachine;
        }

        public virtual UniTask Exit(CancellationToken cts) =>
            UniTask.CompletedTask;

        public virtual UniTask Enter(CancellationToken cts) =>
            UniTask.CompletedTask;
    }
}