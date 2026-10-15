using System;
using UnityEngine;

namespace Features.Sounds
{
    [Serializable]
    public sealed class SoundDefinition
    {
        public SoundId Id => _id;
        public AudioClip Clip => _clip;
        public SoundChannel Channel => _channel;
        public float Volume => Mathf.Clamp01(_volume);
        public bool Loop => _loop;
        public AudioClip[] Variants => _variants;
        public float MinimumInterval => Mathf.Max(0f, _minimumInterval);
        public int MaxLoopInstances => Mathf.Max(1, _maxLoopInstances);

        [SerializeField] private SoundId _id = SoundId.None;
        [SerializeField] private AudioClip _clip;
        [SerializeField] private SoundChannel _channel = SoundChannel.Sfx;
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;
        [SerializeField] private bool _loop;
        [SerializeField] private AudioClip[] _variants = Array.Empty<AudioClip>();
        [SerializeField, Min(0f)] private float _minimumInterval;
        [SerializeField, Min(1)] private int _maxLoopInstances = 2;
    }
}
