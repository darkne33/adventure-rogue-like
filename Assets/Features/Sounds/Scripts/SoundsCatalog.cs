using System.Collections.Generic;
using UnityEngine;

namespace Features.Sounds
{
    [CreateAssetMenu(fileName = "SoundsCatalog", menuName = "Little Rush/Sounds/Catalog")]
    public sealed class SoundsCatalog : ScriptableObject
    {
        public IReadOnlyList<SoundDefinition> Sounds => _sounds;
        public SoundLoopEmitter LoopPrefab => _loopPrefab;

        [SerializeField] private List<SoundDefinition> _sounds = new();
        [SerializeField] private SoundLoopEmitter _loopPrefab;

        private void OnValidate()
        {
            var registeredIds = new HashSet<SoundId>();

            foreach (SoundDefinition sound in _sounds)
            {
                if (sound == null || sound.Id == SoundId.None)
                    continue;

                if (!registeredIds.Add(sound.Id))
                    Debug.LogError($"Sound '{sound.Id}' is registered more than once.", this);
            }
        }
    }
}
