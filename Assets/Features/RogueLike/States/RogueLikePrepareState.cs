using System.Threading;
using CustomPackages.Package.StateMachine.States;
using Cysharp.Threading.Tasks;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core;
using UI;

namespace Core
{
    public class RogueLikePrepareState : State
    {
        private readonly ICharacterFactory _characterFactory;
        private readonly ISceneService<RogueLikeSceneProvider> _sceneService;
        private readonly ICharacterProvider _characterProvider;
        private readonly ILevelFactory _levelFactory;
        private readonly IPanelService _panelService;
        private readonly IRogueLikeRuntimeDataService _rogueLikeRuntimeDataService;
        private readonly LevelsConfiguration _levelsConfiguration;
        private readonly EnemySpawner _enemySpawner;
        private readonly IAbilityChoiceProvider _abilityChoiceProvider;

        public RogueLikePrepareState(ICharacterFactory characterFactory,
            ISceneService<RogueLikeSceneProvider> sceneService, ICharacterProvider characterProvider,
            ILevelFactory levelFactory, IPanelService panelService,
            IRogueLikeRuntimeDataService rogueLikeRuntimeDataService, LevelsConfiguration levelsConfiguration,
            EnemySpawner enemySpawner, IAbilityChoiceProvider abilityChoiceProvider)
        {
            _characterFactory = characterFactory;
            _sceneService = sceneService;
            _characterProvider = characterProvider;
            _levelFactory = levelFactory;
            _panelService = panelService;
            _rogueLikeRuntimeDataService = rogueLikeRuntimeDataService;
            _levelsConfiguration = levelsConfiguration;
            _enemySpawner = enemySpawner;
            _abilityChoiceProvider = abilityChoiceProvider;
        }

        public override async UniTask Enter(CancellationToken cts)
        {
            var panel =
                await _panelService.OpenPanelWithPresenter<CharacterPanel, CharacterPanelPresenter>(PanelName
                    .CharacterPanel);
            panel.Show().Forget();
            
            _abilityChoiceProvider.CreateAllAbilities();
            
            _characterProvider.CharacterFacade =
                await _characterFactory.CreatePlayer(_sceneService.GameSceneComponentsService.CharacterSpawnPoint, cts);

            _sceneService.GameSceneComponentsService.CurrentLevel =
                _levelFactory.CreateLevelView(_rogueLikeRuntimeDataService.CurrentIndexLevel,
                    _sceneService.GameSceneComponentsService.LevelSpawnPoint);

            foreach (var enemyPrefabData in _levelsConfiguration.Levels[_rogueLikeRuntimeDataService.CurrentIndexLevel]
                         .EnemyFactoryConfiguration.EnemyPrefabs)
                await enemyPrefabData.WavesConfigurationContainer.Load(cts);

            _enemySpawner.TrySpawnEnemies(_characterProvider.CharacterFacade);

            Log.Gameplay.Info("RogueLike Prepare State Completed");
        }
    }
}