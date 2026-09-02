namespace Features.Sounds
{
    internal interface ISoundSettingsStorage
    {
        SoundSettingsData Load();
        void Save(SoundSettingsData settings);
    }

    internal struct SoundSettingsData
    {
        public float SfxVolume;
        public float MusicVolume;
        public bool SfxMuted;
        public bool MusicMuted;

        public SoundSettingsData(float sfxVolume, float musicVolume, bool sfxMuted, bool musicMuted)
        {
            SfxVolume = sfxVolume;
            MusicVolume = musicVolume;
            SfxMuted = sfxMuted;
            MusicMuted = musicMuted;
        }
    }
}
