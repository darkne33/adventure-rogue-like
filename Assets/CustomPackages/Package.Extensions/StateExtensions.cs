using System;
using System.Threading;
using CustomPackages.Package.StateMachine.States;
using Cysharp.Threading.Tasks;

namespace CustomPackages.Package.Extensions
{
    public static class StateExtensions 
    {
        public static async UniTask<bool> Delay(this State state, float delay, CancellationToken token) =>
            await UniTask
                .Delay(TimeSpan.FromSeconds(delay), cancellationToken: token)
                .SuppressCancellationThrow();
    }
}