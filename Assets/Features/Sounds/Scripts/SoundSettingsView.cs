using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Zenject;

namespace Features.Sounds
{
    public sealed class SoundSettingsView : MonoBehaviour
    {
        [Serializable]
        private sealed class VolumeRow
        {
            public SoundVolumeCategory Category;
            public UnityEngine.UI.Slider Slider;
            public TMP_Text Value;

            [NonSerialized] public UnityAction<float> Listener;
        }

        [SerializeField] private VolumeRow[] _rows;
        [SerializeField] private UnityEngine.UI.Button _backButton;
        [SerializeField] private bool _handleCancelInput = true;

        [Inject] private ISoundsService _sounds;

        private UnityEngine.UI.Selectable _navigationBack;

        public event Action BackRequested;

        public UnityEngine.UI.Selectable FirstSelectable =>
            _rows != null && _rows.Length > 0 ? _rows[0].Slider : _backButton;

        private void Awake()
        {
            foreach (VolumeRow row in _rows)
            {
                VolumeRow capturedRow = row;
                row.Listener = value => ApplyVolume(capturedRow, value);
                row.Slider.onValueChanged.AddListener(row.Listener);
            }

            _backButton.onClick.AddListener(RequestBack);
            ConfigureNavigation();
        }

        private void OnEnable() => RefreshVolumes();

        private void OnDestroy()
        {
            foreach (VolumeRow row in _rows)
                row.Slider.onValueChanged.RemoveListener(row.Listener);

            _backButton.onClick.RemoveListener(RequestBack);
        }

        private void Update()
        {
            if (!_handleCancelInput)
                return;

            bool escapePressed = Keyboard.current != null &&
                                 Keyboard.current.escapeKey.wasPressedThisFrame;
            bool cancelPressed = Gamepad.current != null &&
                                 Gamepad.current.buttonEast.wasPressedThisFrame;
            if (escapePressed || cancelPressed)
                RequestBack();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            RefreshVolumes();
            FocusFirst();
        }

        public void Hide() => gameObject.SetActive(false);

        public void RefreshVolumes()
        {
            if (_sounds == null)
                return;

            foreach (VolumeRow row in _rows)
            {
                float percent = Mathf.Round(_sounds.GetVolume(row.Category) * 100f);
                row.Slider.SetValueWithoutNotify(percent);
                SetValueLabel(row, percent);
            }
        }

        public void FocusFirst() => UiButtonClickSoundController.Select(FirstSelectable, false);

        public void ConfigureNavigation(UnityEngine.UI.Selectable externalBackButton = null)
        {
            if (externalBackButton != null)
                _navigationBack = externalBackButton;
            else if (_navigationBack == null)
                _navigationBack = _backButton;

            for (int index = 0; index < _rows.Length; index++)
            {
                UnityEngine.UI.Navigation navigation = _rows[index].Slider.navigation;
                navigation.mode = UnityEngine.UI.Navigation.Mode.Explicit;
                navigation.selectOnUp = index > 0 ? _rows[index - 1].Slider : _navigationBack;
                navigation.selectOnDown = index + 1 < _rows.Length ? _rows[index + 1].Slider : _navigationBack;
                navigation.selectOnLeft = null;
                navigation.selectOnRight = null;
                _rows[index].Slider.navigation = navigation;
            }

            if (_navigationBack != null && _rows.Length > 0)
            {
                UnityEngine.UI.Navigation navigation = _navigationBack.navigation;
                navigation.mode = UnityEngine.UI.Navigation.Mode.Explicit;
                navigation.selectOnUp = _rows[_rows.Length - 1].Slider;
                navigation.selectOnDown = _rows[0].Slider;
                navigation.selectOnLeft = null;
                navigation.selectOnRight = null;
                _navigationBack.navigation = navigation;
            }
        }

        private void ApplyVolume(VolumeRow row, float percent)
        {
            _sounds.SetVolume(row.Category, percent * 0.01f);
            SetValueLabel(row, percent);
        }

        private static void SetValueLabel(VolumeRow row, float percent) =>
            row.Value.text = $"{Mathf.RoundToInt(percent)}%";

        private void RequestBack() => BackRequested?.Invoke();
    }
}
