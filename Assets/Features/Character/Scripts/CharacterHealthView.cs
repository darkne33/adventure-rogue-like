using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterHealthView : MonoBehaviour, IHealthView
{
    [SerializeField] private TMP_Text _healthText;
    [SerializeField] private Slider _healthSlider;

    public void UpdateHealth(float currentHealth, float maximumHealth)
    {
        _healthText.text = $"{Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maximumHealth)}";
        _healthSlider.maxValue = maximumHealth;
        _healthSlider.value = currentHealth;
    }
}