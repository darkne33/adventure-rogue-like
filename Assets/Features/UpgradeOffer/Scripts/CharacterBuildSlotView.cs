using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterBuildSlotView : MonoBehaviour
{
    private static readonly Color EmptyFrameColor = new(1f, 1f, 1f, 0.5f);
    private static readonly Color FilledFrameColor = Color.white;

    [SerializeField] private Image _frameImage;
    [SerializeField] private Image _iconImage;
    [SerializeField] private GameObject _levelRoot;
    [SerializeField] private Text _levelText;

    public void SetEmpty()
    {
        if (_frameImage != null)
            _frameImage.color = EmptyFrameColor;

        if (_iconImage != null)
        {
            _iconImage.sprite = null;
            _iconImage.enabled = false;
        }

        if (_levelRoot != null)
            _levelRoot.SetActive(false);
    }

    public void SetUpgrade(UpgradeBuildEntry upgrade)
    {
        if (upgrade?.Ability == null)
        {
            SetEmpty();
            return;
        }

        SetContent(upgrade.Ability.Icon, $"LVL {upgrade.Level}");
    }

    public void SetContent(Sprite icon, string label)
    {
        if (_frameImage != null)
            _frameImage.color = FilledFrameColor;

        if (_iconImage != null)
        {
            _iconImage.sprite = icon;
            _iconImage.enabled = icon != null;
            _iconImage.preserveAspect = true;
        }

        if (_levelText != null)
            _levelText.text = label;

        if (_levelRoot != null)
            _levelRoot.SetActive(string.IsNullOrEmpty(label) == false);
    }
}
