using Cysharp.Threading.Tasks;

namespace Core.Services
{
    public interface IScenesPreloader
    {
        UniTask UnLoadPreLoadedScene();
        void PreLoad(string sceneName);
        UniTask WaitForSceneLoad(string sceneName);
        UniTask UnloadScene(string sceneName);
    }
}