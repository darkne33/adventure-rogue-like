using CustomPackages.Package.StateMachine;
using Zenject;

namespace Core
{
    public class GameStateMachine : ZenjectStateMachine
    {
        protected GameStateMachine(DiContainer container) : base(container)
        {
            Add<BootstrapState>();
            Add<LoadRogueLikeGameSceneState>();
        }
    }
}