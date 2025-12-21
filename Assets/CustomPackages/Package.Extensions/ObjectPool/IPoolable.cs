using System;
using Object = UnityEngine.Object;

namespace CustomPackages.Package.Extensions.ObjectPool
{
    public interface IPoolable<T> where T : Object
    {
        public void SetPool(IGameObjectPool<T> pool);
        public void OnTake();
        public void Release();
        public event Action<T> UnexpectedDestroy;
    }

    public interface IPoolableNotifier<T> where T : Object
    {
        public event Action<T> PoolableTook;
        public event Action<T> PoolableReleased;
    }
}