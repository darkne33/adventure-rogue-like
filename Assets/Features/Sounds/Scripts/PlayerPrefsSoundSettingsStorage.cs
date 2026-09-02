using UnityEngine;

namespace Features.Sounds
{
    internal sealed class PlayerPrefsSoundSettingsStorage : ISoundSettingsStorage
    {
        private const string SfxVolumeKey = "little_rush.audio.sfx.volume";
        private const string MusicVolumeKey = "little_rush.audio.music.volume";
        private const string SfxMutedKey = "little_rush.audio.sfx.muted";
        private const string MusicMutedKey = "little_rush.audio.music.muted";

        public SoundSettingsData Load() =>
            new(
                Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 1f)),
                Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 1f)),
                PlayerPrefs.GetInt(SfxMutedKey, 0) != 0,
                PlayerPrefs.GetInt(MusicMutedKey, 0) != 0);

        public void Save(SoundSettingsData settings)
        {
            PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(settings.SfxVolume));
            PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(settings.MusicVolume));
            PlayerPrefs.SetInt(SfxMutedKey, settings.SfxMuted ? 1 : 0);
            PlayerPrefs.SetInt(MusicMutedKey, settings.MusicMuted ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
