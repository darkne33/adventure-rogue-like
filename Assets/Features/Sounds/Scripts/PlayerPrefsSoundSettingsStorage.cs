using UnityEngine;

namespace Features.Sounds
{
    internal sealed class PlayerPrefsSoundSettingsStorage : ISoundSettingsStorage
    {
        private const string MasterVolumeKey = "little_rush.audio.master.volume";
        private const string SfxVolumeKey = "little_rush.audio.sfx.volume";
        private const string MusicVolumeKey = "little_rush.audio.music.volume";
        private const string AmbienceVolumeKey = "little_rush.audio.ambience.volume";
        private const string UiVolumeKey = "little_rush.audio.ui.volume";
        private const string XpAndGoldVolumeKey = "little_rush.audio.pickups.volume";
        private const string SfxMutedKey = "little_rush.audio.sfx.muted";
        private const string MusicMutedKey = "little_rush.audio.music.muted";

        public SoundSettingsData Load() =>
            new(
                Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 1f)),
                Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 1f)),
                PlayerPrefs.GetInt(SfxMutedKey, 0) != 0,
                PlayerPrefs.GetInt(MusicMutedKey, 0) != 0,
                Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 0.69f)),
                Mathf.Clamp01(PlayerPrefs.GetFloat(AmbienceVolumeKey,
                    PlayerPrefs.GetFloat(SfxVolumeKey, 1f))),
                Mathf.Clamp01(PlayerPrefs.GetFloat(UiVolumeKey,
                    PlayerPrefs.GetFloat(SfxVolumeKey, 1f))),
                Mathf.Clamp01(PlayerPrefs.GetFloat(XpAndGoldVolumeKey,
                    PlayerPrefs.GetFloat(SfxVolumeKey, 1f))));

        public void Save(SoundSettingsData settings)
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, Mathf.Clamp01(settings.MasterVolume));
            PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(settings.SfxVolume));
            PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(settings.MusicVolume));
            PlayerPrefs.SetFloat(AmbienceVolumeKey, Mathf.Clamp01(settings.AmbienceVolume));
            PlayerPrefs.SetFloat(UiVolumeKey, Mathf.Clamp01(settings.UiVolume));
            PlayerPrefs.SetFloat(XpAndGoldVolumeKey, Mathf.Clamp01(settings.XpAndGoldVolume));
            PlayerPrefs.SetInt(SfxMutedKey, settings.SfxMuted ? 1 : 0);
            PlayerPrefs.SetInt(MusicMutedKey, settings.MusicMuted ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
