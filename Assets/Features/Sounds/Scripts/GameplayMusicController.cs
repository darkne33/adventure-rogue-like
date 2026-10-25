using System;
using Zenject;

namespace Features.Sounds
{
    public sealed class GameplayMusicController : IInitializable, IDisposable
    {
        private readonly ISoundsService _sounds;
        private readonly IRogueLikeRuntimeDataService _runtimeData;

        public GameplayMusicController(ISoundsService sounds, IRogueLikeRuntimeDataService runtimeData)
        {
            _sounds = sounds;
            _runtimeData = runtimeData;
        }

        public void Initialize() => _runtimeData.RoomChanged += OnRoomChanged;

        public void Dispose() => _runtimeData.RoomChanged -= OnRoomChanged;

        public void StartBossIntroMusic()
        {
            // Start with the introduction instead of waiting for the outgoing track's fade.
            _sounds.StopMusic();
            _sounds.Play(SoundId.BossMusic);
        }

        private void OnRoomChanged(RoomData previousRoom, RoomData currentRoom)
        {
            if (currentRoom is BossRoomData bossRoom)
            {
                // Completed rooms skip the introduction when revisited.
                if (bossRoom.IsCompleted)
                    _sounds.Play(SoundId.BossMusic);
            }
            else if (previousRoom is BossRoomData)
            {
                _sounds.Play(SoundId.GameplayMusic);
            }
        }
    }
}
