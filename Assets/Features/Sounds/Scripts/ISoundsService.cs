namespace Features.Sounds
{
    public interface ISoundsService
    {
        float SfxVolume { get; }
        float MusicVolume { get; }
        bool SfxMuted { get; }
        bool MusicMuted { get; }
        SoundId CurrentMusic { get; }

        void Play(SoundId soundId);
        void StopAllSfx();
        void StopMusic();
        void SetSfxVolume(float volume);
        void SetMusicVolume(float volume);
        void SetSfxMuted(bool isMuted);
        void SetMusicMuted(bool isMuted);
    }
}
