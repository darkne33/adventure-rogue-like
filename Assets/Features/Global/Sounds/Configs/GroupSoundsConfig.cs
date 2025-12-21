using AYellowpaper.SerializedCollections;
using CustomPackages.Package.Extensions.Other;
using NaughtyAttributes;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core;
using UnityEngine;

namespace Core.Sounds
{
    [CreateAssetMenu(menuName = "Data/Sounds/GroupSoundsConfig")]
    public class GroupSoundsConfig : ScriptableObject
    {
        [field: SerializedDictionary("AudioClipName", "AudioClip")]
        public SerializedDictionary<AudioClipName, AudioClip> AudioClipsAndroid = new();

        [field: SerializedDictionary("AudioClipName", "AudioClip")]
        public SerializedDictionary<AudioClipName, AudioClip> AudioClipsIos = new();
        
        [field: SerializedDictionary("AudioClipName", "Volume")]
        public SerializedDictionary<AudioClipName, float> Volumes = new();

#if UNITY_ANDROID
        public SerializedDictionary<AudioClipName, AudioClip> AudioClips => AudioClipsAndroid;
#else
        public SerializedDictionary<AudioClipName, AudioClip> AudioClips => AudioClipsIos;
#endif

        public void Validate()
        {
            AudioClipsAndroid.Validate(name);
            AudioClipsIos.Validate(name);

            if (Volumes.Count < AudioClipsAndroid.Count) 
                Log.Editor.Error("Volumes.Count < AudioClips.Count");

            if (Volumes.Count < AudioClipsIos.Count) 
                Log.Editor.Error("Volumes.Count < AudioClips.Count");
        }

        [Button]
        public void FillVolumes()
        {
            foreach (var audioClipName in AudioClipsAndroid.Keys)
            {
                if (Volumes.ContainsKey(audioClipName) == false)
                {
                    Volumes.Add(audioClipName, 1);
                }
            }
        }
    }
}