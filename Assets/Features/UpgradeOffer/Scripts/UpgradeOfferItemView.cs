using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeOfferItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameAbilityText;

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

    public void SetupIcon(Sprite icon) =>
        _iconAbility.sprite = icon;

    public void SetupLevel(int lvl) =>
        _newOrLvlAbilityText.text = lvl == 1 ? "NEW" : $"LVL {lvl}";

    public void SetupSkillDescription_1(string statName, int statFrom, int statTo)
    {
        string skillDescription = $"{statName}: {statFrom}% > {statTo}%";
        _skillDescription_1.gameObject.SetActive(true);
        _skillDescription_1.text = skillDescription;
    }

    public void SetupSkillDescription_2(string statName, int statFrom, int statTo)
    {
        string skillDescription = $"{statName}: {statFrom}% > {statTo}%";
        _skillDescription_2.gameObject.SetActive(true);
        _skillDescription_2.text = skillDescription;
    }
}