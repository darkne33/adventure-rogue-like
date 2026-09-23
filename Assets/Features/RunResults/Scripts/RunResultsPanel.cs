using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace Features.RunResults.Scripts
{
    public sealed class RunResultsPanel : MonoBehaviour
    {
        [Header("Presentation")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField] private TMP_Text _statusText;

        [Header("Summary")]
        [SerializeField] private TMP_Text _survivedText;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private TMP_Text _enemiesText;
        [SerializeField] private TMP_Text _goldText;
        [SerializeField] private TMP_Text _silverText;

        [Header("Weapons")]
        [SerializeField] private UnityEngine.UI.ScrollRect _weaponsScroll;
        [SerializeField] private RectTransform _weaponRowsRoot;
        [SerializeField] private RunResultsWeaponRow _weaponRowTemplate;
        [SerializeField] private GameObject _emptyWeaponsMessage;

        [Header("Actions")]
        [SerializeField] private UnityEngine.UI.Button _retryButton;
        [SerializeField] private UnityEngine.UI.Button _menuButton;

        private readonly List<RunResultsWeaponRow> _weaponRows = new();

        public event Action RetryRequested;
        public event Action MenuRequested;

        public CanvasGroup CanvasGroup => _canvasGroup;
        public RectTransform ContentRoot => _contentRoot;
        public UnityEngine.UI.Button RetryButton => _retryButton;

        private void Awake()
        {
            _retryButton.onClick.AddListener(RequestRetry);
            _menuButton.onClick.AddListener(RequestMenu);
            _weaponRowTemplate.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _retryButton.onClick.RemoveListener(RequestRetry);
            _menuButton.onClick.RemoveListener(RequestMenu);
        }

        public void SetResults(RunResultsData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            _survivedText.text = FormatDuration(data.SurvivedSeconds);
            _levelText.text = data.LevelReached.ToString(CultureInfo.InvariantCulture);
            _enemiesText.text = data.EnemiesDefeated.ToString("N0", CultureInfo.InvariantCulture);
            _goldText.text = data.GoldEarned.ToString("N0", CultureInfo.InvariantCulture);
            _silverText.text = data.SilverEarned.ToString("N0", CultureInfo.InvariantCulture);

            int count = data.Weapons?.Count ?? 0;
            while (_weaponRows.Count < count)
            {
                RunResultsWeaponRow row = Instantiate(_weaponRowTemplate, _weaponRowsRoot);
                row.name = $"WeaponRow_{_weaponRows.Count + 1}";
                _weaponRows.Add(row);
            }

            for (int index = 0; index < _weaponRows.Count; index++)
            {
                bool visible = index < count;
                _weaponRows[index].gameObject.SetActive(visible);
                if (visible)
                    _weaponRows[index].SetResult(data.Weapons[index], index);
            }

            _emptyWeaponsMessage.SetActive(count == 0);
            _weaponRowTemplate.gameObject.SetActive(false);
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_weaponRowsRoot);
            _weaponsScroll.StopMovement();
            _weaponsScroll.verticalNormalizedPosition = 1f;
            SetStatus(string.Empty);
        }

        public void SetStatus(string value) =>
            _statusText.text = value ?? string.Empty;

        public void SetButtonsInteractable(bool interactable)
        {
            _retryButton.interactable = interactable;
            _menuButton.interactable = interactable;
        }

        internal static string FormatDuration(float seconds)
        {
            int wholeSeconds = Mathf.FloorToInt(Mathf.Max(0f, seconds));
            int hours = wholeSeconds / 3600;
            int minutes = wholeSeconds / 60 % 60;
            int remainder = wholeSeconds % 60;
            return hours > 0
                ? $"{hours:00}:{minutes:00}:{remainder:00}"
                : $"{minutes:00}:{remainder:00}";
        }

        private void RequestRetry() => RetryRequested?.Invoke();
        private void RequestMenu() => MenuRequested?.Invoke();
    }
}
