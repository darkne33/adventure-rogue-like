using Cysharp.Threading.Tasks;

namespace Core.Sounds
{
    public interface ISoundsService
    {
        UniTask Initialize();
        void PlayAudio(SoundsGroupName soundsGroupName, AudioClipName audioClipName, SoundType soundType = SoundType.Sound);
        public UniTask MuteSounds(bool isMute);
        public UniTask MuteMusic(bool isMute);
        public bool SoundsIsMute { get; }
        public bool MusicIsMute { get; }
    }
}