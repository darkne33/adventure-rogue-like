using System;
using System.Collections.Generic;
using System.Threading;
using CustomPackages.Package.Extensions.ObjectPool;
using Cysharp.Threading.Tasks;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core;
using Zenject;

namespace Core.Services
{
    public class EffectsService : IEffectsService, IDisposable
    {
        [Inject] private DiContainer _container;

        private readonly EffectsConfig _config;
        private readonly Dictionary<EffectName, IGameObjectPool<EffectPlayer>> _effectsPool = new();

        public EffectsService(EffectsConfig config)
        {
            _config = config;
        }

        public async UniTask WarmUp(CancellationToken cts)
        {
            await _config.Load(cts);
            foreach (var pair in _config.Effects)
            {
                if (_config.Effects.TryGetValue(pair.Key, out var effect)) 
                    AddEffectToPool(effect.Get(), pair.Key);
            }
        }

        public void Dispose()
        {
            foreach (var pair in _effectsPool)
            {
                pair.Value.ReleaseAllElements();
            }

            _config.CleanUp();
        }

        public void AddEffect(EffectPlayer effectPlayer, EffectName effectName)
        {
            if (_effectsPool.ContainsKey(effectName))
            {
                Log.Gameplay.Warn($"Already exists effect {effectName}");
                RemoveEffect(effectName);
            }

            AddEffectToPool(effectPlayer, effectName);
        }

        public void RemoveEffect(EffectName effectName)
        {
            if (_effectsPool.ContainsKey(effectName))
            {
                _effectsPool[effectName].ReleaseAllElements();
                _effectsPool[effectName].DestroyAll();
                _effectsPool.Remove(effectName);
            }
        }

        public EffectPlayer GetEffect(EffectName effectName)
        {
            if (_effectsPool.TryGetValue(effectName, out var effectPlayer))
                return effectPlayer.Get();

            return _config.Effects.TryGetValue(effectName, out var effect)
                ? GetFromPool(effect.Get(), effectName)
                : null;
        }

        private EffectPlayer GetFromPool(EffectPlayer effect, EffectName effectName, int defaultCount = 3)
        {
            if (_effectsPool.TryGetValue(effectName, out IGameObjectPool<EffectPlayer> pool))
                return pool.Get();

            AddEffectToPool(effect, effectName, defaultCount);
            return _effectsPool[effectName].Get();
        }

        private void AddEffectToPool(EffectPlayer effect, EffectName effectName, int defaultCount = 1)
        {
            IGameObjectPool<EffectPlayer> gameObjectPool = new GameObjectPool<EffectPlayer>(effect, defaultCount);
            gameObjectPool.InitRoot(_container.DefaultParent);
            gameObjectPool.WarmUp();
            _effectsPool.Add(effectName, gameObjectPool);
        }
    }
}