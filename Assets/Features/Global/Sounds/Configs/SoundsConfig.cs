using AYellowpaper.SerializedCollections;
using Core.Services;
using UnityEngine;
using UnityEngine.Audio;

namespace Core.Sounds
{
    [CreateAssetMenu(menuName = "Data/Sounds/Config")]
    public class SoundsConfig : ScriptableObject
    {
        [field: SerializedDictionary("GroupName", "Container")]
        public SerializedDictionary<SoundsGroupName, AddressableLoadContainerScriptableObject> AudioGroupContainers;

        public AudioMixerGroup MusicAudioMixerGroup => _musicAudioMixerGroup;
        public AudioMixerGroup SfxAudioMixerGroup => _sfxAudioMixerGroup;
        
        [SerializeField] private AudioMixerGroup _musicAudioMixerGroup;
        [SerializeField] private AudioMixerGroup _sfxAudioMixerGroup;
    }
}