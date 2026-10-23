namespace Features.Sounds
{
    internal interface ISoundSettingsStorage
    {
        SoundSettingsData Load();
        void Save(SoundSettingsData settings);
    }

    internal struct SoundSettingsData
    {
        public float MasterVolume;
        public float SfxVolume;
        public float MusicVolume;
        public float AmbienceVolume;
        public float UiVolume;
        public float XpAndGoldVolume;
        public bool SfxMuted;
        public bool MusicMuted;

        public SoundSettingsData(float sfxVolume, float musicVolume, bool sfxMuted, bool musicMuted,
            float masterVolume, float ambienceVolume, float uiVolume, float xpAndGoldVolume)
        {
            MasterVolume = masterVolume;
            SfxVolume = sfxVolume;
            MusicVolume = musicVolume;
            AmbienceVolume = ambienceVolume;
            UiVolume = uiVolume;
            XpAndGoldVolume = xpAndGoldVolume;
            SfxMuted = sfxMuted;
            MusicMuted = musicMuted;
        }
    }
}
