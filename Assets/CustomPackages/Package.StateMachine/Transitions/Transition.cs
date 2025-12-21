using System;

namespace CustomPackages.Package.StateMachine.Transitions
{
    public abstract class Transition : ITransition
    {
        public Type FromState { get; }
        public Type ToState { get; }

        public Transition(Type fromState, Type toState)
        {
            FromState = fromState;
            ToState = toState;
        }

        public abstract bool GetPermission();
    }
}