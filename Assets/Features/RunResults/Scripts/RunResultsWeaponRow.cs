using System.Globalization;
using TMPro;
using UnityEngine;

namespace Features.RunResults.Scripts
{
    public sealed class RunResultsWeaponRow : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image _background;
        [SerializeField] private UnityEngine.UI.Image _icon;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private TMP_Text _damageText;
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private TMP_Text _dpsText;
        [SerializeField] private Color _evenColor = new(0.10f, 0.16f, 0.15f, 0.9f);
        [SerializeField] private Color _oddColor = new(0.07f, 0.12f, 0.12f, 0.9f);

        public void SetResult(RunWeaponResult result, int index)
        {
            _background.color = index % 2 == 0 ? _evenColor : _oddColor;
            _icon.sprite = result.Icon;
            _icon.enabled = result.Icon != null;
            _nameText.text = result.Name;
            _levelText.text = result.Level.ToString(CultureInfo.InvariantCulture);
            _damageText.text = result.Damage.ToString("N0", CultureInfo.InvariantCulture);
            _timeText.text = RunResultsPanel.FormatDuration(result.ActiveSeconds);
            _dpsText.text = result.Dps.ToString("N1", CultureInfo.InvariantCulture);
        }
    }
}
