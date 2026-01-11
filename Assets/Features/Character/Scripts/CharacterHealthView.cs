using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterHealthView : MonoBehaviour
{
    [SerializeField] private TMP_Text _healthText;
    [SerializeField] private Slider _healthSlider;

    public void UpdateHealth(int currentHealth, int maximumHealth)
    {
        _healthText.text = $"{currentHealth}/{maximumHealth}";
        _healthSlider.maxValue = maximumHealth;
        _healthSlider.value = currentHealth;
    }
}