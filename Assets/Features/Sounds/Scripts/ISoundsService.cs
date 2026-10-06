using UnityEngine;

namespace Features.Sounds
{
    public interface ISoundsService
    {
        float SfxVolume { get; }
        float MusicVolume { get; }
        bool SfxMuted { get; }
        bool MusicMuted { get; }
        SoundId CurrentMusic { get; }

        void SetUiSounds(SoundsCatalog catalog);
        void Play(SoundId soundId);
        void PlaySfx(AudioClip clip, float volume = 1f);
        void StopAllSfx();
        void StopMusic();
        void SetSfxVolume(float volume);
        void SetMusicVolume(float volume);
        void SetSfxMuted(bool isMuted);
        void SetMusicMuted(bool isMuted);
    }
}
