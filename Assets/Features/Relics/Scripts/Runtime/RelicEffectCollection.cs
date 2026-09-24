using System.Collections;
using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Features.Relics.Scripts
{
    internal abstract class RelicVisualInstance
    {
        public RelicRuntimeState State;
        public RelicEffectDefinition Effect;
        public EffectPlayer Visual;
        public Vector3 Position;

        public void Release()
        {
            if (Visual != null)
                Visual.Release();
            Visual = null;
        }
    }

    // Every mechanic owns its collection. Version changes detect removal from reentrant hit/kill callbacks.
    internal sealed class RelicEffectCollection<T> : IReadOnlyList<T> where T : RelicVisualInstance
    {
        private readonly List<T> _items = new();
        public int Version { get; private set; }
        public int Count => _items.Count;
        public T this[int index] => _items[index];

        public void Add(T item) => _items.Add(item);
        public int FindIndex(System.Predicate<T> predicate) => _items.FindIndex(predicate);

        public void RemoveAt(int index)
        {
            T item = _items[index];
            _items.RemoveAt(index);
            item.Release();
        }

        public void RemoveOwner(RelicRuntimeState state)
        {
            Version++;
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                if (_items[i].State == state)
                    RemoveAt(i);
            }
        }

        public void Clear()
        {
            Version++;
            for (int i = _items.Count - 1; i >= 0; i--)
                RemoveAt(i);
        }

        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
