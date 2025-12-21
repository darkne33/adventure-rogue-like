using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core;
using UI;
using Zenject;

namespace Core.Services
{
    public class ScenesPreloader : IScenesPreloader
    {
        [Inject] private ISceneLoader _sceneLoader;
        [Inject] private IPanelService _panelService;

        private readonly Dictionary<string, UniTask> _preloadedScenes = new();
        private readonly List<string> _scenesInUnloading = new();

        public UniTask WaitForSceneLoad(string sceneName)
        {
            if (_preloadedScenes.ContainsKey(sceneName) == false)
            {
                Log.Gameplay.Debug("load scene without preload");
                PreLoad(sceneName);
            }

            return _preloadedScenes[sceneName];
        }

        public void PreLoad(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return;

            LoadScene(sceneName);
        }

        public async UniTask UnloadScene(string sceneName)
        {
            await _sceneLoader.UnloadScene(sceneName);
            _preloadedScenes.Remove(sceneName);
        }

        private void LoadScene(string sceneName)
        {
            if (_preloadedScenes.ContainsKey(sceneName) == false)
                _preloadedScenes.Add(sceneName, _sceneLoader.LoadSceneFromAddressable(sceneName));
        }

        public async UniTask UnLoadPreLoadedScene()
        {
            List<string> keysRemoved = new();
            List<UniTask> tasks = new();
            foreach (var pair in _preloadedScenes.Where(pair => _sceneLoader.HasActiveScene(pair.Key)))
            {
                if (_scenesInUnloading.Contains(pair.Key))
                {
                    continue;
                }
                
                keysRemoved.Add(pair.Key);
                _scenesInUnloading.Add(pair.Key);
                
                tasks.Add(UnloadScene(pair.Key));
            }

            await UniTask.WhenAll(tasks);
            foreach (var key in keysRemoved)
            {
                _preloadedScenes.Remove(key);
                _scenesInUnloading.Remove(key);
            }
        }
    }
}