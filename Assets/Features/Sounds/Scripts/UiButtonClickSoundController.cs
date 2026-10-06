using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Zenject;

namespace Features.Sounds
{
    [DisallowMultipleComponent]
    public sealed class UiButtonClickSoundController : MonoBehaviour,
        ISelectHandler,
        IPointerDownHandler, IPointerUpHandler, ISubmitHandler
    {
        [SerializeField] private SoundId _sound = SoundId.UiClick;
        [SerializeField] private SoundId _hoverSound = SoundId.UiHover;
        [SerializeField] private SoundId _valueSound = SoundId.UiAdjust;

        private ISoundsService _soundsService;
        private UnityEngine.UI.Selectable _selectable;
        private UnityEngine.UI.Button _button;
        private UnityEngine.UI.Toggle _toggle;
        private UnityEngine.UI.Slider _slider;
        private UnityEngine.UI.Scrollbar _scrollbar;
        private bool _pointerHeld;
        private bool _buttonListenerAdded;

        [Inject]
        private void Construct(ISoundsService soundsService) =>
            _soundsService = soundsService;

        private void Awake()
        {
            _selectable = GetComponent<UnityEngine.UI.Selectable>();
            _button = _selectable as UnityEngine.UI.Button;
            _toggle = _selectable as UnityEngine.UI.Toggle;
            _slider = _selectable as UnityEngine.UI.Slider;
            _scrollbar = _selectable as UnityEngine.UI.Scrollbar;
        }

        private void OnEnable()
        {
            if (_button != null && !HasPersistentClickListener())
            {
                _button.onClick.AddListener(OnClick);
                _buttonListenerAdded = true;
            }
            if (_toggle != null)
                _toggle.onValueChanged.AddListener(OnToggleChanged);
            if (_slider != null)
                _slider.onValueChanged.AddListener(OnValueChanged);
            if (_scrollbar != null)
                _scrollbar.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnDisable()
        {
            if (_button != null && _buttonListenerAdded)
                _button.onClick.RemoveListener(OnClick);
            if (_toggle != null)
                _toggle.onValueChanged.RemoveListener(OnToggleChanged);
            if (_slider != null)
                _slider.onValueChanged.RemoveListener(OnValueChanged);
            if (_scrollbar != null)
                _scrollbar.onValueChanged.RemoveListener(OnValueChanged);

            _buttonListenerAdded = false;
            _pointerHeld = false;
        }

        // Keep the existing Inspector events working; do not subscribe a second time.
        public void OnClick() => _soundsService?.Play(_sound);

        public void OnSelect(BaseEventData eventData)
        {
            if (CanInteract())
                _soundsService?.Play(_hoverSound);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            _pointerHeld = CanInteract();
            PlayDisabledFeedback();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                _pointerHeld = false;
        }

        public void OnSubmit(BaseEventData eventData) => PlayDisabledFeedback();

        private void PlayDisabledFeedback()
        {
            // A blocked parent CanvasGroup is a transition, not an invalid choice.
            if (_selectable != null && _selectable.IsActive() && !_selectable.interactable &&
                ParentGroupsAllowInput())
                _soundsService?.Play(SoundId.UiError);
        }

        private bool ParentGroupsAllowInput()
        {
            foreach (CanvasGroup group in GetComponentsInParent<CanvasGroup>())
            {
                if (!group.interactable)
                    return false;
                if (group.ignoreParentGroups)
                    break;
            }
            return true;
        }

        private bool CanInteract() => _selectable != null && _selectable.IsActive() &&
                                      _selectable.IsInteractable();

        private void OnToggleChanged(bool value) => PlayValueFeedback();
        private void OnValueChanged(float value) => PlayValueFeedback();

        private void PlayValueFeedback()
        {
            bool focused = EventSystem.current != null &&
                           EventSystem.current.currentSelectedGameObject == gameObject;
            if (CanInteract() && (_pointerHeld || focused))
                _soundsService?.Play(_valueSound);
        }

        private bool HasPersistentClickListener()
        {
            for (int i = 0; i < _button.onClick.GetPersistentEventCount(); i++)
            {
                if (_button.onClick.GetPersistentTarget(i) == this &&
                    _button.onClick.GetPersistentMethodName(i) == nameof(OnClick) &&
                    _button.onClick.GetPersistentListenerState(i) != UnityEventCallState.Off)
                    return true;
            }
            return false;
        }
    }
}
