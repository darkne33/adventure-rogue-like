using UnityEngine;
using UnityEngine.UI;

public class HealthView : MonoBehaviour, IHealthView
{
    [SerializeField] private Slider _healthSlider;
    
    public void UpdateHealth(float currentHealth, float maximumHealth)
    {
        _healthSlider.maxValue = maximumHealth;
        _healthSlider.value = currentHealth;
    }
}