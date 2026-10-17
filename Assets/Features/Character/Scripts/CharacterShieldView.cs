using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterShieldView : MonoBehaviour, IShieldView
{
    [SerializeField] private TMP_Text _shieldText;
    [SerializeField] private Slider _shieldSlider;

    public void UpdateShield(float currentShield, float maximumShield)
    {
        bool hasShield = maximumShield > 0f;
        if (gameObject.activeSelf != hasShield)
            gameObject.SetActive(hasShield);

        if (hasShield == false)
            return;

        float availableShield = Mathf.Floor((currentShield + 0.001f) / CharacterHealthRules.HalfHeart) *
                                CharacterHealthRules.HalfHeart;
        _shieldText.text = $"{availableShield:0.#}/{maximumShield:0.#}";
        _shieldSlider.maxValue = maximumShield;
        _shieldSlider.value = currentShield;
    }
}
