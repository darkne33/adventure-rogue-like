using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Services
{
    public interface IIconService
    {
        UniTask<Sprite> Get(RewardBase rewardBase, CancellationToken cts);
        UniTask<Sprite> Get(Type type, CancellationToken cts);
        UniTask Initialize(CancellationToken cts);
        void Add(Type type, Sprite sprite);
        void Remove(Type type);
    }
}