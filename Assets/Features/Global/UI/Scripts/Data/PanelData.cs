using System;
using UnityEngine.AddressableAssets;

namespace UI
{
    [Serializable]
    public struct PanelData
    {
#if UNITY_EDITOR
        public string Id;
#endif
        public PanelName PanelName;
        public PanelLocation PanelLocation;
        public bool IsLazy;
        public AssetReference AssetReference;
    }
}