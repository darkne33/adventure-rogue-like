using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using Features.Sounds;
using Zenject;
using UnityEngine;

namespace UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class PanelAnimationsMonoComponent : MonoBehaviour
    {
        public bool IsPlaying { get; protected set; }

        [SerializeReference, SubclassSelector] public IPanelAnimation _panelAnimation;

        [SerializeField, ReadOnly] private CanvasGroup _canvasGroup;
        [SerializeField] private bool _playUiSounds;

        [Inject] private ISoundsService _soundsService;
        private bool _isShown;

        public virtual async UniTask Show()
        {
            PlayVisibilitySound(true);
            IsPlaying = true;
            SetInputState(true);
            await _panelAnimation.Show();
            IsPlaying = false;
        }

        public void ForceShow()
        {
            PlayVisibilitySound(true);
            SetInputState(true);
            _panelAnimation.ForceShow();
        }

        public virtual async UniTask Hide()
        {
            PlayVisibilitySound(false);
            IsPlaying = true;
            await _panelAnimation.Hide();
            SetInputState(false);
            IsPlaying = false;
        }

        public virtual void ForceHide()
        {
            PlayVisibilitySound(false);
            SetInputState(false);
            _panelAnimation.ForceHide();
        }

        private void PlayVisibilitySound(bool shown)
        {
            if (_isShown == shown)
                return;

            _isShown = shown;
            if (_playUiSounds)
                _soundsService?.Play(shown ? SoundId.UiOpen : SoundId.UiClose);
        }

        public void SetInputState(bool interactable)
        {
            _canvasGroup.interactable = interactable;
            _canvasGroup.blocksRaycasts = interactable;
        }

        private void Awake() => 
            _panelAnimation.Initialize();

        private void OnDestroy() =>
            _panelAnimation.Cleanup();

        private void OnValidate() => 
            _canvasGroup ??= GetComponent<CanvasGroup>();
    }
}