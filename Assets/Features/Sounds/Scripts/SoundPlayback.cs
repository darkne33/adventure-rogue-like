using UnityEngine;

namespace Features.Sounds
{
    [DisallowMultipleComponent]
    public sealed class SoundPlayback : MonoBehaviour
    {
        public AudioSource GameSounds => _gameSounds;
        public AudioSource Music => _music;
        public AudioSource Ambience => _ambience;
        public AudioSource Ui => _ui;
        public AudioSource XpAndGold => _xpAndGold;
        public AudioLowPassFilter MusicLowPass => _musicLowPass;
        public float PausedMusicCutoff => Mathf.Clamp(_pausedMusicCutoff, 10f, 22000f);
        public float MusicFadeDuration => Mathf.Max(0f, _musicFadeDuration);
        public float PauseFilterDuration => Mathf.Max(0f, _pauseFilterDuration);

        [SerializeField] private AudioSource _gameSounds;
        [SerializeField] private AudioSource _music;
        [SerializeField] private AudioSource _ambience;
        [SerializeField] private AudioSource _ui;
        [SerializeField] private AudioSource _xpAndGold;
        [SerializeField] private AudioLowPassFilter _musicLowPass;
        [SerializeField, Min(10f)] private float _pausedMusicCutoff = 900f;
        [SerializeField, Min(0f)] private float _musicFadeDuration = 0.35f;
        [SerializeField, Min(0f)] private float _pauseFilterDuration = 0.15f;
    }
}
