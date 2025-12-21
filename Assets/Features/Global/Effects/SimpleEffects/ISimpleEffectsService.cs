using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Services
{
    public interface ISimpleEffectsService
    {
        UniTask Warmup(CancellationToken cts);
        GameObject GetPileOfAsh(Transform root);
    }
}