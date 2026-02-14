using System;
using System.Threading;
using CustomPackages.Package.StateMachine.States;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Features.Enemies.Scripts;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core;
using UI;

namespace Core
{
    public class RogueLikeSpawnEnemyWaveState : State
    {
        private readonly LevelsConfiguration _levelsConfiguration;
        private readonly EnemySpawner _enemySpawner;
        private readonly IRogueLikeRuntimeDataService _rogueLikeRuntimeDataService;
        private readonly ICharacterProvider _characterProvider;
        private readonly EnemiesWaveObserver _enemiesWaveObserver;
        private readonly IPanelService _panelService;

        public RogueLikeSpawnEnemyWaveState(LevelsConfiguration levelsConfiguration, EnemySpawner enemySpawner,
            IRogueLikeRuntimeDataService rogueLikeRuntimeDataService, ICharacterProvider characterProvider,
            EnemiesWaveObserver enemiesWaveObserver, IPanelService panelService)
        {
            _levelsConfiguration = levelsConfiguration;
            _enemySpawner = enemySpawner;
            _rogueLikeRuntimeDataService = rogueLikeRuntimeDataService;
            _characterProvider = characterProvider;
            _enemiesWaveObserver = enemiesWaveObserver;
            _panelService = panelService;
        }

        public override async UniTask Enter(CancellationToken cts)
        {
            Log.Gameplay.Info("RogueLikeSpawnEnemyWaveState Completed");

            foreach (var enemyPrefabData in _levelsConfiguration.Levels[_rogueLikeRuntimeDataService.CurrentIndexLevel]
                         .EnemyFactoryConfiguration.EnemyPrefabs)
                await enemyPrefabData.WavesConfigurationContainer.Load(cts);

            var characterPanel =  _panelService.GetPanelPresenter<CharacterPanelPresenter>(PanelName.CharacterPanel).Panel;
            characterPanel.WaveAlertText.text = $"Wave {_enemiesWaveObserver.CurrentWave + 1}";
            
            await characterPanel.WaveAlertText.DOFade(1, 1f).ToUniTask(cancellationToken: cts);
            await characterPanel.WaveAlertText.DOFade(0, 0.5f).ToUniTask(cancellationToken: cts);
            
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: cts);

            _enemySpawner.TrySpawnEnemies(_characterProvider.CharacterFacade, _enemiesWaveObserver.CurrentWave);
        }
    }
}