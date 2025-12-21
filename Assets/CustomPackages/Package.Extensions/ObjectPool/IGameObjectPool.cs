using System.Collections.Generic;
using UnityEngine;

namespace CustomPackages.Package.Extensions.ObjectPool
{
    public interface IGameObjectPool<T> where T : Object
    {
        void InitRoot(Transform defaultRoot);
        void WarmUp();
        T Get();
        T Get(Transform parent);
        T Get(Vector3 position, Transform parent);
        void Release(T element);
        void ReleaseAllElements();
        int Capacity { get; }
        List<T> GetAll();
        void DestroyAll();
    }
}