using Core.Services;
using Features.Leaderboard;
using Features.Quests.Scripts;
using Features.Sounds;
using Infrastructure.SaveSystem;
using UnityEngine;
using Zenject;

namespace Core.Installer
{
    public class ProjectInstaller : MonoInstaller
    {
        [SerializeField] private EffectsConfig _effectsConfig;
        [SerializeField] private CharacterExpConfig _characterExpConfig;
        [SerializeField] private SoundsCatalog _soundsCatalog;
        [SerializeField] private GameObject _debugConsolePrefab;
        
        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 120;
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_debugConsolePrefab != null)
                Instantiate(_debugConsolePrefab, transform);
#endif
        }
        
        public override void InstallBindings()
        {
            Container.Install<AddressableInstaller>();
            Container.Bind<SoundsCatalog>().FromInstance(_soundsCatalog).AsSingle();
            Container.Install<SoundsInstaller>();
            
            Container.BindInterfacesAndSelfTo<GameStateMachine>().AsSingle();
            
            Container.Bind<IScenesPreloader>().To<ScenesPreloader>().AsSingle();
            Container.Bind<ISceneLoader>().To<SceneLoader>().AsSingle();
   
            Container.Bind<IPlayerSaveLoadService>().To<PlayerSaveLoadService>().AsSingle();
            Container.Bind<ILeaderboardService>().To<PlayFabLeaderboardService>().AsSingle();
            
            Container.Bind<IDynamicEffectsService>().To<DynamicEffectsService>().AsSingle();
            
            Container.Bind<IGameModeService>().To<GameModeService>().AsSingle();
            
            Container.Bind<EffectsConfig>().FromInstance(_effectsConfig).AsSingle();
            Container.Bind<IEffectsService>().To<EffectsService>().AsSingle();

            Container.Bind<PlayerWallet>().AsSingle();
            ProgressionConfiguration progressionConfiguration =
                Resources.Load<ProgressionConfiguration>(ProgressionConfiguration.ResourcePath);
            if (progressionConfiguration == null)
                throw new System.InvalidOperationException("The demo progression configuration is missing from Resources.");
            Container.Bind<ProgressionConfiguration>().FromInstance(progressionConfiguration).AsSingle();
            Container.BindInterfacesAndSelfTo<QuestService>().AsSingle().NonLazy();
            
            Container.Bind<CharacterExpConfig>().FromInstance(_characterExpConfig).AsSingle();
            Container.Bind<ICharacterLevelService>().To<CharacterLevelService>().AsSingle();
            Container.Bind<ICursorService>().To<CursorService>().AsSingle();
            Container.Bind<ITimeScaleService>().To<TimeScaleService>().AsSingle();
            Container.Bind<IPauseService>().To<PauseService>().AsSingle();
            Container.Bind<PauseEntityDistributor>().AsSingle();
            Container.Bind<RunRestartService>().AsSingle();
        }
    }
}
