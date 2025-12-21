using System;
using CustomPackages.Package.Extensions.ObjectPool;
using UnityEngine;

namespace Other.Effects.PoolableEffectTypes
{
    public class FlyingPoolableEffect: MonoBehaviour, IPoolable<FlyingPoolableEffect>
    {
        public event Action<FlyingPoolableEffect> UnexpectedDestroy;
        private IGameObjectPool<FlyingPoolableEffect> _pool;
        
        public void SetPool(IGameObjectPool<FlyingPoolableEffect> pool) =>
            _pool = pool;

        public void OnTake()
        {
            
        }

        public void Release() => 
            _pool.Release(this);
        
        private void OnDestroy() => 
            UnexpectedDestroy?.Invoke(this);
    }
}