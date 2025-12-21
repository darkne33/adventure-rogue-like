using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using Zenject;

namespace CustomPackages.Package.Extensions.ObjectPool
{
    public class GameObjectPool<T> : IGameObjectPool<T> where T : Object
    {
        public int Capacity => _freeObjects.Count + _usedObjects.Count;

        private readonly HashSet<T> _freeObjects = new HashSet<T>();
        private readonly HashSet<T> _usedObjects = new HashSet<T>();

        private Transform _defaultRoot;
        private readonly T _prefab;
        private readonly int _defaultCount;
        
        private DiContainer Container => _container ?? ProjectContext.Instance.Container;

        private readonly DiContainer _container;

        public GameObjectPool(T prefab, int defaultCount, DiContainer diContainer = null)
        {
            _prefab = prefab;
            _defaultCount = defaultCount;
            _container = diContainer;
        }

        public void InitRoot(Transform defaultRoot)
        {
            _defaultRoot = new GameObject($"{typeof(T).Name}_Pool_From_{_prefab.name}").transform;
            _defaultRoot.SetParent(defaultRoot);
            _defaultRoot.transform.localScale = Vector3.one;
        }

        public void WarmUp()
        {
            for (int i = 0; i < _defaultCount; i++)
            {
                CreateObject();
            }
        }

        public T Get()
        {
            T freeObject = default;

            if (NoObjectsInPool()) freeObject = CreateObject();
            else freeObject = _freeObjects.FirstOrDefault();

            freeObject.GameObject().SetActive(true);

            _freeObjects.Remove(freeObject);
            _usedObjects.Add(freeObject);

            if (freeObject is IPoolable<T> poolable) poolable.OnTake();

            return freeObject;
        }

        public T Get(Transform parent)
        {
            var obj = Get();

            obj.GameObject().transform.SetParent(parent);

            return obj;
        }

        public T Get(Vector3 position, Transform parent)
        {
            var obj = Get(parent);
            obj.GameObject().transform.position = position;
            return obj;
        }

        public void Release(T element)
        {
            if (_usedObjects.Contains(element) == false) return;

            _usedObjects.Remove(element);

            if (element is IPoolable<T> poolable) poolable.Release();

            element.GameObject().transform.SetParent(_defaultRoot);
            element.GameObject().SetActive(false);

            _freeObjects.Add(element);
        }

        public void ReleaseAllElements()
        {
            var objectsToRelease = new List<T>(_usedObjects);
            foreach (var item in objectsToRelease)
            {
                Release(item);
            }
        }

        public List<T> GetAll()
        {
            List<T> lst = new List<T>();
            lst.AddRange(_freeObjects);
            lst.AddRange(_usedObjects);
            return lst;
        }

        public void DestroyAll() => 
            Object.Destroy(_defaultRoot.gameObject);

        private bool NoObjectsInPool()
        {
            return _freeObjects.Count == 0;
        }

        private T CreateObject()
        {
            var obj = Container.InstantiatePrefabForComponent<T>(_prefab, _defaultRoot);
            obj.GameObject().SetActive(false);

            _freeObjects.Add(obj);

            if (obj is IPoolable<T> poolable)
            {
                poolable.SetPool(this);
                poolable.UnexpectedDestroy += UnexpectedDestroy;
            }

            return obj;
        }

        private void UnexpectedDestroy(T obj)
        {
            _freeObjects.Remove(obj);
            _usedObjects.Remove(obj);
        }
    }
}