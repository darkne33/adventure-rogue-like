using System;
using UnityEngine;
using Zenject;

namespace Features.Bosses.Scripts
{
    public sealed class BossFactory
    {
        private readonly DiContainer _container;

        public BossFactory(DiContainer container)
        {
            _container = container;
        }

        public BossFacade Create(BossFacade prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            BossFacade boss = _container.InstantiatePrefabForComponent<BossFacade>(
                prefab.gameObject, position, rotation, null);
            boss.InitializeBoss();
            return boss;
        }
    }
}
