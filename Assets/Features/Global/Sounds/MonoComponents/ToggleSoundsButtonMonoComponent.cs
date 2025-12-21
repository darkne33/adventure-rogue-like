using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Core.Sounds
{
    public class ToggleSoundsButtonMonoComponent : MonoBehaviour
    {
        [Inject] private ISoundsService _soundsService;

        [SerializeField] private Button _toggleButton;
        [SerializeField] private bool _isMute;
        [SerializeField] private GameObject _toggleAudioView;

        private void Start() =>
            _toggleAudioView.GetComponent<ToggleAudioViewMonoComponent>()
                .ToggleOn(_soundsService.SoundsIsMute, 0f)
                .Forget();

        private void OnEnable()
        {
            _toggleButton.onClick.AddListener(() =>
            {
                _soundsService.MuteSounds(_isMute).Forget();
                _toggleAudioView.GetComponent<ToggleAudioViewMonoComponent>().ToggleOn(_isMute, 0.2f).Forget();
            });
        }

        private void OnDisable() =>
            _toggleButton.onClick.RemoveAllListeners();
    }
}