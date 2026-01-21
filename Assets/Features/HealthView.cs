using UnityEngine;
using UnityEngine.UI;

public class HealthView : MonoBehaviour, IHealthView
{
    [SerializeField] private Slider _healthSlider;

    public void UpdateHealth(int currentHealth, int maximumHealth)
    {
        _healthSlider.maxValue = maximumHealth;
        _healthSlider.value = currentHealth;
    }
}