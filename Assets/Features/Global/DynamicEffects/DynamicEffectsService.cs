using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core;
using Zenject;

namespace Core.Services
{
    public class DynamicEffectsService : IDynamicEffectsService
    {
        [Inject] private IEffectsService _effectsService;

        private readonly Dictionary<ContentType, DynamicEffectsConfig> _configs = new();

        public async UniTask Initialize(ContentType contentType, DynamicEffectsConfig config, CancellationToken token)
        {
            if (_configs.ContainsKey(contentType))
            {
                Log.Gameplay.Warn($"Attempty to add exist effects for {contentType} with {config}");
                return;
            }

            _configs[contentType] = config;
            await UniTask.WhenAll(Enumerable.Select(config.Effects, pair => pair.Value.Load(token)).ToList());
            foreach (var item in config.Effects)
            {
                _effectsService.AddEffect(item.Value.Get(), item.Key);
            }
        }

        public void Cleanup(ContentType contentType)
        {
            var config = _configs[contentType];
            foreach (var pair in config.Effects)
            {
                _effectsService.RemoveEffect(pair.Key);
                pair.Value.CleanUp();
            }

            _configs.Remove(contentType);
        }
    }
}