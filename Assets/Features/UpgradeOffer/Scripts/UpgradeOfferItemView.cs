using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeOfferItemView : MonoBehaviour
{
    private const string UpgradeToValueColor = "#6CFF7A";

    [SerializeField] private TMP_Text _nameAbilityText;
    [SerializeField] private TMP_Text _rarityText;

    [SerializeField] private Image _itemSpriteImage;
    [SerializeField] private Image _backgroundSpriteImage;
    [SerializeField] private Image _iconAbility;

    [SerializeField] private TMP_Text _newOrLvlAbilityText;

    [SerializeField] private TMP_Text _skillDescription_1;
    [SerializeField] private TMP_Text _skillDescription_2;

    public void DeactivateSkillsDescriptions()
    {
        _skillDescription_1.gameObject.SetActive(false);
        _skillDescription_2.gameObject.SetActive(false);
    }

    public void SetupName(string nameAbility) =>
        _nameAbilityText.text = nameAbility;

    public void SetupRarity(UpgradeRarity rarity)
    {
        _rarityText.gameObject.SetActive(true);
        _rarityText.text = $"{rarity}";
        _rarityText.color = GetRarityColor(rarity);
    }

    public void HideRarity() =>
        _rarityText.gameObject.SetActive(false);

    public void SetupIcon(Sprite icon) =>
        _iconAbility.sprite = icon;

    public void SetupSprites(UpgradeItemSpriteSet spriteSet)
    {
        if (spriteSet == null)
            return;

        if (_itemSpriteImage != null && spriteSet.ItemSprite != null)
            _itemSpriteImage.sprite = spriteSet.ItemSprite;

        if (_backgroundSpriteImage != null && spriteSet.BackgroundSprite != null)
            _backgroundSpriteImage.sprite = spriteSet.BackgroundSprite;
    }

    public void SetupLevel(int lvl) =>
        _newOrLvlAbilityText.text = lvl == 1 ? "NEW" : $"LVL {lvl}";

    public void SetupLevel(int lvl, bool isAcquired) =>
        _newOrLvlAbilityText.text = isAcquired ? $"LVL {lvl}" : "NEW";

    public void SetupSkillDescription_1(string statName, int statFrom, int statTo)
    {
        string skillDescription = $"{statName}: {statFrom}% > {ColorizeUpgradeToValue($"{statTo}%")}";
        _skillDescription_1.gameObject.SetActive(true);
        _skillDescription_1.text = skillDescription;
    }

    public void SetupSkillDescription_1(AbilityUpgradePreview preview)
    {
        _skillDescription_1.gameObject.SetActive(true);
        _skillDescription_1.text = GetSkillDescription(preview);
    }

    public void SetupSkillDescription_2(string statName, int statFrom, int statTo)
    {
        string skillDescription = $"{statName}: {statFrom}% > {ColorizeUpgradeToValue($"{statTo}%")}";
        _skillDescription_2.gameObject.SetActive(true);
        _skillDescription_2.text = skillDescription;
    }

    public void SetupSkillDescription_2(AbilityUpgradePreview preview)
    {
        _skillDescription_2.gameObject.SetActive(true);
        _skillDescription_2.text = GetSkillDescription(preview);
    }

    private static string GetSkillDescription(AbilityUpgradePreview preview)
    {
        string statTo = ColorizeUpgradeToValue(FormatValue(preview.StatTo, preview.Suffix));

        if (preview.HasStatFrom == false)
            return $"{preview.StatName}: {statTo}";

        return $"{preview.StatName}: {FormatValue(preview.StatFrom, preview.Suffix)} > " +
               $"{statTo}";
    }

    private static string ColorizeUpgradeToValue(string value) =>
        $"<color={UpgradeToValueColor}>{value}</color>";

    private static string FormatValue(float value, string suffix)
    {
        string format = Mathf.Abs(value - Mathf.Round(value)) < 0.01f ? "0" : "0.##";
        return $"{value.ToString(format)}{suffix}";
    }

    private static Color GetRarityColor(UpgradeRarity rarity) =>
        rarity switch
        {
            UpgradeRarity.Common => Color.white,
            UpgradeRarity.Rare => new Color(0.35f, 0.75f, 1f),
            UpgradeRarity.Epic => new Color(0.75f, 0.45f, 1f),
            UpgradeRarity.Legendary => new Color(1f, 0.72f, 0.22f),
            _ => Color.white
        };
}
