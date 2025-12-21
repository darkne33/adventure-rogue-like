using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Effects;
using Other.Effects.FlyingEffects;
using UI;
using UnityEngine;
using Zenject;

namespace Core.Services
{
    public class FlyingEffectsService : IFlyingEffectsService
    {
        public LevelFlyingEffect LevelEffect { get; set; }

        [Inject] private DiContainer _container;
        [Inject] private IUIFactory _uiFactory;
        [Inject] private IPanelService _panelService;

        private Transform EffectRoot => _container.DefaultParent;
        private readonly FlyingEffectsConfig _effectsConfig;

        public FlyingEffectsService(FlyingEffectsConfig effectsConfig)
        {
            _effectsConfig = effectsConfig;
        }

        public UniTask Initialize(CancellationToken cts)
        {
            LevelEffect = _container.Instantiate<LevelFlyingEffect>(new object[] { _effectsConfig, EffectRoot });

            return UniTask.WhenAll(
                LevelEffect.Initialize(cts),
                _effectsConfig.FlyingLinearElementsPrefab.Load(cts),
                _effectsConfig.FlyingBoilingElementsPrefab.Load(cts),
                _effectsConfig.FlyingBoilingEnergyElementPrefab.Load(cts));
        }

        /// <summary>
        /// UniTask done when first element pumps
        /// </summary>
        public async UniTask PlayLinearFlyingElements(Transform from, Transform to, Sprite icon,
            CancellationToken token, int count)
        {
            await PlayFlyingElement(_effectsConfig.FlyingLinearElementsPrefab, from, to, icon, count);
            await UniTask.Delay(TimeSpan.FromSeconds(0.35f), cancellationToken: token);
        }

        public UniTask PlayBoilingFlyingElements(Transform from, Transform to, Sprite icon, long rewardValue,
            bool withoutPump = false) =>
            PlayFlyingElement(_effectsConfig.FlyingBoilingElementsPrefab, from, to, icon, rewardValue,
                withoutPump);

        public async UniTask PlayBoilingFlyingElementsPlinko(Transform from, Transform to, Sprite icon, long rewardValue,
            CancellationToken token, bool withoutPump = false)
        {
            await PlayBoilingFlyingElements(from, to, icon, rewardValue, withoutPump);
        }

        public UniTask PlayBoilingFlyingEnergyElement(Transform from, Transform to, Sprite icon, long rewardValue,
            bool withoutPump = false) =>
            PlayFlyingElement(_effectsConfig.FlyingBoilingEnergyElementPrefab, from, to, icon, rewardValue, withoutPump);
        

        private UniTask PlayFlyingElement(AddressableLoadContainerGameObject flyingElementsPrefab,
            Transform from, Transform to, Sprite icon, long rewardValue, bool withoutPump = false)
        {
            var instance = _container.InstantiatePrefab(flyingElementsPrefab.Get(),
                _uiFactory.UIRoot);
            return instance.GetComponent<FlyingElementsMonoComponent>()
                .Play(from, to, icon.texture, rewardValue, withoutPump);
        }
    }
}