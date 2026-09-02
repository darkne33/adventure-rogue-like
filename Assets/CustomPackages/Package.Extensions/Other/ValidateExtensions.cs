using System;
using AYellowpaper.SerializedCollections;
using Core;
using Core.Services;
using UI;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CustomPackages.Package.Extensions.Other
{
    public static class ValidateExtensions
    {
        public static void Validate(this AssetReference assetReference, string name)
        {
#if UNITY_EDITOR
            if (assetReference.editorAsset == null)
            {
                Debug.LogWarning($"No asset reference assigned in {name}");
            }
#endif
        }

        public static void Validate(this AddressableLoadContainer container, string scriptableObjectName)
        {
            if (container.AssetReference == null)
            {
                Debug.LogWarning($"no asset reference assigned in {scriptableObjectName}");
            }
            else
            {
                container.AssetReference.Validate(scriptableObjectName);
            }
        }

        public static void Validate(this PanelData panelData, string scriptableObjectName)
        {
            panelData.AssetReference.Validate(scriptableObjectName);
            ((int)panelData.PanelName).Validate<PanelName>(scriptableObjectName);
        }
        
        public static void Validate(this SerializedDictionary<string, AddressableLoadContainerGameObject> dictionary,
            string scriptableObjectName)
        {
            foreach (var pair in dictionary)
            {
                pair.Value.Validate(scriptableObjectName);
            }
        }
        
        public static void Validate<T>(this int stateValue, string scriptableObjectName) where T : Enum
        {
            if (!Enum.IsDefined(typeof(T), stateValue))
            {
                Debug.LogError($"Wrong Enum Value in {scriptableObjectName}");
            }
        }
        

        public static void Validate(this SerializedDictionary<ContentType, AddressableLoadContainerSprite> dictionary,
            string scriptableObjectName)
        {
            foreach (var pair in dictionary)
            {
                pair.Value.Validate(scriptableObjectName);
                ((int)pair.Key).Validate<ContentType>(scriptableObjectName);
            }
        }
        
        public static void Validate(
            this SerializedDictionary<EffectName, AddressableLoadContainerEffectPlayer> dictionary,
            string scriptableObjectName)
        {
            foreach (var pair in dictionary)
            {
                pair.Value.Validate(scriptableObjectName);
                ((int)pair.Key).Validate<EffectName>(scriptableObjectName);
            }
        }
    }
}