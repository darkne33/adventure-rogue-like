using UnityEngine;
using Zenject;

namespace Features.Sounds
{
    public sealed class UiButtonClickSoundController : MonoBehaviour
    {
        [SerializeField] private SoundId _sound = SoundId.UiClick;

        private ISoundsService _soundsService;

        [Inject]
        private void Construct(ISoundsService soundsService) =>
            _soundsService = soundsService;

        public void OnClick() =>
            _soundsService.Play(_sound);
    }
}
