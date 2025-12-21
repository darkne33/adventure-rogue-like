using System.Threading;
using Cysharp.Threading.Tasks;
using Other.Effects.FlyingEffects;
using UnityEngine;

namespace Core.Services
{
    public interface IFlyingEffectsService
    {
        UniTask Initialize(CancellationToken cts);
        LevelFlyingEffect LevelEffect { get; set; }
        UniTask PlayLinearFlyingElements(Transform from, Transform to, Sprite icon, CancellationToken token, int count);
        UniTask PlayBoilingFlyingElements(Transform from, Transform to, Sprite icon, long rewardValue,
            bool withAsyncPump = false);
        UniTask PlayBoilingFlyingElementsPlinko(Transform from, Transform to, Sprite icon, long rewardValue,
            CancellationToken token, bool withoutPump = false);
        UniTask PlayBoilingFlyingEnergyElement(Transform from, Transform to, Sprite icon, long rewardValue,
            bool withAsyncPump = false);
    }
}