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
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
            await _panelAnimation.Show();
            IsPlaying = false;
        }

        public void ForceShow()
        {
            SetInputState(true);
            _panelAnimation.ForceShow();
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }

        public virtual async UniTask Hide()
        {
            IsPlaying = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            await _panelAnimation.Hide();
            SetInputState(false);
            IsPlaying = false;
        }

        public virtual void ForceHide()
        {
            SetInputState(false);
            _panelAnimation.ForceHide();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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