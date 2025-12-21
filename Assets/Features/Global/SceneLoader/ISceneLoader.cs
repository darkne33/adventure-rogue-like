using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    public interface ISceneLoader
    {
        T GetGameSceneComponentsProvider<T>(string sceneName) where T : MonoBehaviour;
        UniTask LoadSceneFromAddressable(string sceneName);
        UniTask UnloadScene(string sceneName);
        bool HasActiveScene(string sceneName);
        void UnloadBootstrapScene();
    }
}