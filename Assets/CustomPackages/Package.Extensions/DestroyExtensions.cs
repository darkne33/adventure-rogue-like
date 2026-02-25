using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CustomPackages.Package.Extensions
{
    public static class DestroyExtensions
    {
        public static async UniTask DestroyAfterDelay(GameObject gameObject, float delay, CancellationToken cancellationToken)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);
            Object.Destroy(gameObject);
        }
    }
}