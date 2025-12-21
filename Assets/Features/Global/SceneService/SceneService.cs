using Infrastructure.SceneProvider;
using Zenject;

namespace Core.Services
{
    public class SceneService<T> : ISceneService<T> where T : GameSceneComponentsProvider
    {
        public T GameSceneComponentsService { get; }

        [Inject]
        public SceneService(ISceneLoader sceneLoader, string sceneName)
        {
            GameSceneComponentsService = sceneLoader.GetGameSceneComponentsProvider<T>(sceneName);
            GameSceneComponentsService.DisableScene();
        }
    }
}