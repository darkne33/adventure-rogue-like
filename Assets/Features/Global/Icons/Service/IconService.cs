using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core;
using UnityEngine;

namespace Core.Services
{
    public class IconService : IIconService
    {
        private readonly IconsConfig _iconsConfig;
        private Dictionary<Type, Sprite> _icons;

        public IconService(IconsConfig iconsConfig)
        {
            _iconsConfig = iconsConfig;
        }

        public async UniTask Initialize(CancellationToken cts)
        {
            await UniTask.WhenAll(Enumerable.Select(_iconsConfig.Setups, setup => setup.Icon.Load(cts)).ToList());
            _icons = _iconsConfig.Setups.ToDictionary(x => x.RewardType.GetType(), x => x.Icon.Get());
        }

        public void Add(Type type, Sprite sprite) => 
            _icons.Add(type, sprite);

        public void Remove(Type type) =>
            _icons.Remove(type);

        public async UniTask<Sprite> Get(Type type, CancellationToken cts)
        {
            if (_icons.ContainsKey(type) == false)
            {
                Log.Gameplay.Warn($"Start waiting load icon for {type}");
                await UniTask.WaitWhile(() => _icons.ContainsKey(type) == false, cancellationToken: cts);
            }

            return _icons[type];
        }

        public UniTask<Sprite> Get(RewardBase rewardBase, CancellationToken cts) =>
            Get(rewardBase.GetType(), cts);
    }
}