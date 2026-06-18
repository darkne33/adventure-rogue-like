using System;
using System.Threading;
using Core;
using Core.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Relics.Scripts
{
    public interface IRelicVisualEffectService
    {
        UniTask PlayMeteorImpact(Vector3 position, float radius, CancellationToken token = default);
        UniTask PlayExplosiveCrate(Vector3 position, float radius, Action<Vector3> onDetonate,
            CancellationToken token = default);
    }

    public sealed class RelicVisualEffectService : IRelicVisualEffectService
    {
        private readonly IEffectsService _effectsService;

        public RelicVisualEffectService(IEffectsService effectsService)
        {
            _effectsService = effectsService;
        }

        public async UniTask PlayMeteorImpact(Vector3 position, float radius, CancellationToken token = default)
        {
            float scale = Mathf.Max(0.8f, radius);
            await TryPlayConfiguredEffect(EffectName.RelicMeteorImpact, position + Vector3.up * 0.4f,
                scale, token);
        }

        public async UniTask PlayExplosiveCrate(Vector3 position, float radius, Action<Vector3> onDetonate,
            CancellationToken token = default)
        {
            EffectPlayer configuredEffect = _effectsService.GetEffect(EffectName.RelicExplosiveCrate);
            if (configuredEffect != null)
            {
                configuredEffect.transform.position = position + Vector3.up * 0.45f;
                configuredEffect.transform.localScale = Vector3.one * 0.75f;
                configuredEffect.PlayWithoutRelease();

                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: token);
                    onDetonate?.Invoke(configuredEffect.transform.position);
                }
                finally
                {
                    configuredEffect.Release();
                }

                return;
            }

            await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: token);
            onDetonate?.Invoke(position);
        }

        private async UniTask<bool> TryPlayConfiguredEffect(EffectName effectName, Vector3 position, float scale,
            CancellationToken token)
        {
            EffectPlayer effect = _effectsService.GetEffect(effectName);
            if (effect == null)
                return false;

            effect.transform.position = position;
            effect.transform.localScale = Vector3.one * scale;
            await effect.PlayAsync(token);
            return true;
        }
    }
}
