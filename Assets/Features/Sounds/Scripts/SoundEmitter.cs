using UnityEngine;
using Zenject;

namespace Features.Sounds
{
    [DisallowMultipleComponent]
    public sealed class SoundEmitter : MonoBehaviour
    {
        [SerializeField] private SoundId _sound = SoundId.None;

        private ISoundsService _soundsService;

        [Inject]
        private void Construct(ISoundsService soundsService) =>
            _soundsService = soundsService;

        public void Play()
        {
            if (_soundsService == null)
            {
                Debug.LogWarning($"{nameof(SoundEmitter)} on '{name}' was not injected.", this);
                return;
            }

            _soundsService.Play(_sound);
        }
    }
}
