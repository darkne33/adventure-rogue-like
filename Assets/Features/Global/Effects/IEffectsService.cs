using System.Threading;
using Cysharp.Threading.Tasks;

namespace Core.Services
{
    public interface IEffectsService
    {
        EffectPlayer GetEffect(EffectName effectName);
        UniTask PlayConfettiOnBordersOfScreen(CancellationToken token, bool isOnce);
        void AddEffect(EffectPlayer effectPlayer, EffectName effectName);
        void RemoveEffect(EffectName effectName);
        UniTask WarmUp(CancellationToken cts);
    }
}