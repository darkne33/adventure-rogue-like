using CustomPackages.Package.StateMachine.States;
using Zenject;

namespace CustomPackages.Package.StateMachine
{
    public class ZenjectStateMachine : StateMachine
    {
        private readonly DiContainer _container;

        protected ZenjectStateMachine(DiContainer container) => 
            _container = container;

        protected void Add<T>() where T : IExitableState =>
            AddState(Create<T>());

        private TState Create<TState>() where TState : IExitableState =>
            _container.Instantiate<TState>();

        public T Resolve<T>() where T : class =>
            _container.TryResolve<T>();

        public T ResolveWithId<T>(string id) where T : class =>
            _container.TryResolveId<T>(id);
    }
}