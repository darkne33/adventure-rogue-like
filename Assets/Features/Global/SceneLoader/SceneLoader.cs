using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Core
{
    public class SceneLoader : ISceneLoader
    {
        private readonly List<SceneInstance> _activeScenes = new();

        public async UniTask LoadSceneFromAddressable(string sceneName)
        {
            Log.Gameplay.Debug($"Preload {sceneName}");
            _activeScenes.Add(await Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Additive));
        }

        public async UniTask ReloadSceneFromAddressable(string sceneName)
        {
            Log.Gameplay.Debug($"Reload {sceneName}");
            SceneInstance sceneInstance =
                await Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Single);

            if (!sceneInstance.Scene.IsValid() || !sceneInstance.Scene.isLoaded)
                throw new InvalidOperationException(
                    $"Addressable scene '{sceneName}' was not loaded during reload.");

            _activeScenes.Clear();
            _activeScenes.Add(sceneInstance);
        }

        public async UniTask UnloadScene(string sceneName)
        {
            var sceneInstance = _activeScenes.First(x => x.Scene.name == sceneName);
            await Addressables.UnloadSceneAsync(sceneInstance);

            if (sceneInstance.Scene.isLoaded)
                throw new InvalidOperationException(
                    $"Addressable scene '{sceneName}' could not be unloaded.");

            _activeScenes.Remove(sceneInstance);
        }

        public T GetGameSceneComponentsProvider<T>(string sceneName) where T : MonoBehaviour
        {
            var gameScene = SceneManager.GetSceneByName(sceneName);
            var rootGameObjects = gameScene.GetRootGameObjects();
            foreach (var obj in rootGameObjects)
            {
                var component = obj.GetComponent<T>();
                if (component != null)
                    return component;
            }

            Log.Editor.Error($"No exist component in scene {sceneName}");
            return null;
        }

        public bool HasActiveScene(string sceneName) =>
            _activeScenes.Any(x => x.Scene.name == sceneName &&
                                   x.Scene.IsValid() && x.Scene.isLoaded);

        public void UnloadBootstrapScene()
        {
            Scene bootstrapScene = SceneManager.GetSceneByName(SceneNames.BootstrapScene);
            if (bootstrapScene.IsValid() && bootstrapScene.isLoaded)
                SceneManager.UnloadSceneAsync(bootstrapScene);
        }
    }
}
