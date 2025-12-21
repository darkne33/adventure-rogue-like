using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Core.Sounds
{
    public class ToggleMusicButtonMonoComponent : MonoBehaviour
    {
        [SerializeField] private Button _toggleButton;
        [SerializeField] private bool _isMute;
        [SerializeField] private GameObject _toggleAudioView;

        [Inject] private ISoundsService _soundsService;

        private void Start() =>
            _toggleAudioView.GetComponent<ToggleAudioViewMonoComponent>()
                .ToggleOn(_soundsService.MusicIsMute, 0f)
                .Forget();

        private void OnEnable()
        {
            _toggleButton.onClick.AddListener(() =>
            {
                _soundsService.MuteMusic(_isMute).Forget();
                _toggleAudioView.GetComponent<ToggleAudioViewMonoComponent>().ToggleOn(_isMute, 0.2f).Forget();
            });
        }

        private void OnDisable() => 
            _toggleButton.onClick.RemoveAllListeners();
    }
}