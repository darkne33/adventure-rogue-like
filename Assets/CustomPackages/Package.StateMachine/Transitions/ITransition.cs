using System;

namespace CustomPackages.Package.StateMachine.Transitions
{
    public interface ITransition
    {
        public Type FromState { get; }
        public Type ToState { get; }
        public bool GetPermission();
    }
}