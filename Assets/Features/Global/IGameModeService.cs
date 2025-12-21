using CustomPackages.Package.StateMachine;
using Zenject;

namespace Core
{
    public interface IGameModeService
    {
        public void Add<T>(DiContainer diContainer) where T : ZenjectStateMachine;
        public T Get<T>() where T : ZenjectStateMachine;
    }
}