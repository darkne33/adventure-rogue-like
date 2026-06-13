using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;

namespace UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class PanelAnimationsMonoComponent : MonoBehaviour
    {
        public bool IsPlaying { get; protected set; }

        [SerializeReference, SubclassSelector] public IPanelAnimation _panelAnimation;

        [SerializeField, ReadOnly] private CanvasGroup _canvasGroup;

        public virtual async UniTask Show()
        {
            IsPlaying = true;
            SetInputState(true);
            await _panelAnimation.Show();
            IsPlaying = false;
        }

        public void ForceShow()
        {
            SetInputState(true);
            _panelAnimation.ForceShow();
        }

        public virtual async UniTask Hide()
        {
            IsPlaying = true;
            await _panelAnimation.Hide();
            SetInputState(false);
            IsPlaying = false;
        }

        public virtual void ForceHide()
        {
            SetInputState(false);
            _panelAnimation.ForceHide();
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