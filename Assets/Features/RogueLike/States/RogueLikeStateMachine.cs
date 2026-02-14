using CustomPackages.Package.StateMachine;
using Zenject;

namespace Core
{
    public class RogueLikeStateMachine : ZenjectStateMachine
    {
        protected RogueLikeStateMachine(DiContainer container) : base(container)
        {
            Add<RogueLikePrepareState>();
            Add<RogueLikeSpawnEnemyWaveState>();
            Add<RogueLikeCleanUpState>();
        }
    }
}