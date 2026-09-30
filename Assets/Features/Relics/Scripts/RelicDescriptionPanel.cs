using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UI;
using UnityEngine;
using Zenject;

namespace Features.Relics.Scripts
{
    public sealed class RelicDescriptionPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _relicName;
        [SerializeField] private TMP_Text _relicGrade;
        [SerializeField] private TMP_Text _relicDescription;
        [SerializeField] private UnityEngine.UI.Image _relicIcon;
        [SerializeField] private UnityEngine.UI.Button _takeRelicButton;
        [SerializeField] private UnityEngine.UI.Button _skipRelicButton;

        [Inject] private ICursorService _cursorService;

        private PanelAnimationsMonoComponent _panelAnimations;

        public event Action TakeRequested;
        public event Action SkipRequested;

        private void Awake()
        {
            _panelAnimations = GetComponent<PanelAnimationsMonoComponent>();
            _takeRelicButton.onClick.AddListener(HandleTakeRequested);
            if (_skipRelicButton != null)
                _skipRelicButton.onClick.AddListener(HandleSkipRequested);
        }

        private void OnDestroy()
        {
            _takeRelicButton.onClick.RemoveListener(HandleTakeRequested);
            if (_skipRelicButton != null)
                _skipRelicButton.onClick.RemoveListener(HandleSkipRequested);
        }

        public void Show(RelicDefinition relic)
        {
            if (relic == null)
                return;

            _relicName.text = relic.DisplayName;
            _relicGrade.text = relic.Rarity.ToString();
            _relicGrade.color = RelicRarityPalette.GetColor(relic.Rarity);
            _relicDescription.text = relic.Description;

            _relicIcon.sprite = relic.Icon;
            _relicIcon.preserveAspect = true;
            _relicIcon.gameObject.SetActive(relic.Icon != null);

            gameObject.SetActive(true);
            _panelAnimations.Show().Forget();
            _cursorService.ShowUiCursor();
        }

        public async UniTask Hide()
        {
            await _panelAnimations.Hide();
            gameObject.SetActive(false);
            _cursorService.ShowGameplayCursor();
        }

        private void HandleTakeRequested() =>
            TakeRequested?.Invoke();

        private void HandleSkipRequested() =>
            SkipRequested?.Invoke();
    }
}
