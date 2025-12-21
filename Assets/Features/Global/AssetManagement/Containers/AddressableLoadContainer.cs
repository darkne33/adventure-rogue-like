using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Zenject;

namespace Core
{
    [Serializable]
    public abstract class AddressableLoadContainer
    {
        public AssetReference AssetReference => _assetReference;
        
        [SerializeField] protected AssetReference _assetReference;

        protected static IAddressableLoadService LoadService
        {
            get
            {
                _loadService ??= ProjectContext.Instance.Container.Resolve<IAddressableLoadService>();
                return _loadService;
            }
        }

        private static IAddressableLoadService _loadService;

#if UNITY_EDITOR
        public void Set(AssetReference assetReference) =>
            _assetReference = assetReference;
#endif
    }
}