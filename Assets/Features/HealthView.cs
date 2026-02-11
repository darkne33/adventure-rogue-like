using Core;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class HealthView : MonoBehaviour, IHealthView
{
    [SerializeField] private RectTransform _root;
    [SerializeField] private Slider _healthSlider;
    
    [Inject] private ICameraService _cameraService;
    
    private void Update()
    {
        _root.LookAt(new Vector3(_cameraService.MainCamera.transform.position.x, _root.position.y,
            _cameraService.MainCamera.transform.position.z));
    }

    public void UpdateHealth(int currentHealth, int maximumHealth)
    {
        _healthSlider.maxValue = maximumHealth;
        _healthSlider.value = currentHealth;
    }
}