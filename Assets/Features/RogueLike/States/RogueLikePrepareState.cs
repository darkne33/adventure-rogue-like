using System.Threading;
using CustomPackages.Package.StateMachine.States;
using Cysharp.Threading.Tasks;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core;

namespace Core
{
    public class RogueLikePrepareState : State
    {
        private readonly ICharacterFactory _characterFactory;
        private readonly ISceneService<RogueLikeSceneProvider> _sceneService;

        public RogueLikePrepareState(ICharacterFactory characterFactory,
            ISceneService<RogueLikeSceneProvider> sceneService)
        {
            _characterFactory = characterFactory;
            _sceneService = sceneService;
        }

        public override async UniTask Enter(CancellationToken cts)
        {
            await _characterFactory.CreatePlayer(_sceneService.GameSceneComponentsService.CharacterSpawnPoint, cts);
            
            Log.Gameplay.Info("RogueLike Prepare State Completed");
        }
    }
}