using System.Collections.Generic;
using System.Threading;
using Core;
using Core.Services;
using CustomPackages.Package.Extensions.ObjectPool;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Other.Effects.PoolableEffectTypes;
using UI;
using UnityEngine;
using Zenject;

namespace Other.Effects.FlyingEffects
{
    public class LevelFlyingEffect
    {
        [Inject] private IPanelService _panelService;
        
        private readonly AddressableLoadContainerGameObject _levelPrefabContainer;
        private readonly List<FlyingPoolableEffect> _flyingPrefabs = new();
        private readonly Transform _effectRoot;

        private IGameObjectPool<FlyingPoolableEffect> _levelEffectPool;

        private const int _effectZOffset = -15;
        public float SpeedDivider { get; set; }

        public LevelFlyingEffect(FlyingEffectsConfig effectsConfig, Transform effectRoot)
        {
            _effectRoot = effectRoot;
            _levelPrefabContainer = effectsConfig.LevelPrefabContainer;
        }

        public async UniTask Initialize(CancellationToken cts)
        {
            await LevelEffectPoolInit(cts);
        }

        public void ReleaseAllObjects()
        {
            foreach (var levelPrefab in _flyingPrefabs)
            {
                _levelEffectPool.Release(levelPrefab);
            }
        }

        private void Release(FlyingPoolableEffect poolObject)
        {
            _flyingPrefabs.Remove(poolObject);
            _levelEffectPool.Release(poolObject);
        }

        private async UniTask IconAnimation(CancellationToken token, Transform uiIconTransform)
        {
            var sequence = DOTween.Sequence();
            sequence.Append(uiIconTransform.DOScale(1.15f, 0.12f)
                .SetEase(Ease.InOutFlash));
            sequence.Append(uiIconTransform.DOScale(1f, 0.16f).SetEase(Ease.InOutFlash));
            if (await sequence.ToUniTask(cancellationToken: token).SuppressCancellationThrow())
            {
                uiIconTransform.localScale = Vector3.one;
            }
        }

        private async UniTask ItemScaleAnimation(Transform levelPrefabRectTransform, CancellationToken token)
        {
            var sequence = DOTween.Sequence();
            sequence.Append(levelPrefabRectTransform.DOScale(Vector3.one * 1.4f, 0.3f)
                .SetEase(Ease.OutSine).From(Vector3.one * .4f));
            sequence.Append(levelPrefabRectTransform.DOScale(Vector3.one * 0.6f, 0.5f)
                .SetEase(Ease.Linear));
            await sequence.Play().ToUniTask(cancellationToken: token);
        }

        private async UniTask FlyTransform(Transform itemToMove, Vector3 from, Vector3 to, CancellationToken token) =>
            await itemToMove.DOMove(to, 0.8f).From(from).SetEase(Ease.InCubic)
                .Play().ToUniTask(cancellationToken: token);

        private async UniTask LevelEffectPoolInit(CancellationToken cts)
        {
            await _levelPrefabContainer.Load(cts);

            if (_levelPrefabContainer.Get().TryGetComponent(out FlyingPoolableEffect levelEffectPrefab))
            {
                _levelEffectPool = new GameObjectPool<FlyingPoolableEffect>(levelEffectPrefab, 4);
                _levelEffectPool.InitRoot(_effectRoot);
                _levelEffectPool.WarmUp();
            }
        }
    }
}