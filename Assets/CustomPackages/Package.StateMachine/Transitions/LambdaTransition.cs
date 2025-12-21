using System;

namespace CustomPackages.Package.StateMachine.Transitions
{
    public class LambdaTransition : Transition
    {
        private readonly Func<bool> _permission;

        public LambdaTransition(Type fromState, Type toState, Func<bool> permission) : base(fromState, toState)
        {
            _permission = permission;
        }

        public override bool GetPermission() => _permission.Invoke();
    }
}