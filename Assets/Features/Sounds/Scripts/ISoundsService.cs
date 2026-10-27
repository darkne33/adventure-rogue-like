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
        void SetGameplaySounds(SoundsCatalog catalog);
        void SetMusicSounds(SoundsCatalog catalog);
        void Play(SoundId soundId);
        void PlayLoop(SoundId soundId, Transform owner);
        void StopGameplayLoops();
        void PlaySfx(AudioClip clip, float volume = 1f);
        void StopGameplaySfx();
        void StopAllSfx();
        void StopMusic();
        void RestartMusic(SoundId soundId);
        float GetVolume(SoundVolumeCategory category);
        void SetVolume(SoundVolumeCategory category, float volume);
        void SetSfxVolume(float volume);
        void SetMusicVolume(float volume);
        void SetSfxMuted(bool isMuted);
        void SetMusicMuted(bool isMuted);
    }
}
