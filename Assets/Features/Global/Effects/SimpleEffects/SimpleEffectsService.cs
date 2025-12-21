using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Services
{
    public class SimpleEffectsService : ISimpleEffectsService
    {
        private readonly SimpleEffectsConfig _config;

        public SimpleEffectsService(SimpleEffectsConfig config)
        {
            _config = config;
        }

        public UniTask Warmup(CancellationToken cts) =>
            _config.Load(cts);

        public GameObject GetPileOfAsh(Transform root) =>
            Object.Instantiate(_config.PileOfAsh.Get(), root);
    }
}