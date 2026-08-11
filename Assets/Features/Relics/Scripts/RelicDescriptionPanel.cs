using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Features.Relics.Scripts
{
    public sealed class RelicDescriptionPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _relicName;
        [SerializeField] private TMP_Text _relicGrade;
        [SerializeField] private TMP_Text _relicDescription;
        [SerializeField] private Image _relicIcon;
        [SerializeField] private Button _takeRelicButton;

        [Inject] private ICursorService _cursorService;

        private PanelAnimationsMonoComponent _panelAnimations;

        public event Action TakeRequested;

        private void Awake()
        {
            _panelAnimations = GetComponent<PanelAnimationsMonoComponent>();
            _takeRelicButton.onClick.AddListener(HandleTakeRequested);
        }

        private void OnDestroy() =>
            _takeRelicButton.onClick.RemoveListener(HandleTakeRequested);

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
            _panelAnimations.ForceShow();
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
    }
}
