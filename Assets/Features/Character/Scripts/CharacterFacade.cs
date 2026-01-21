using Core;
using NaughtyAttributes;
using UI;
using UnityEngine;
using Zenject;

[RequireComponent(typeof(Rigidbody))]
public class CharacterFacade : MonoBehaviour
{
    public HealthSystem HealthSystem => _healthSystem;
    public Rigidbody Rigidbody => _rigidbody;

    [Inject] private CharacterSettingsConfiguration _characterSettingsConfiguration;
    [Inject] private CharacterCameraSettingsConfiguration _characterCameraSettingsConfiguration;

    [Inject] private ICameraService _cameraService;
    [Inject] private IPanelService _panelService;

    [HorizontalLine] private CharacterMoveSystem _moveSystem;
    private CharacterCameraMoveSystem _cameraSystem;
    private HealthSystem _healthSystem;
    private CharacterFxSystem _characterFxSystem;
    private IHealthView _healthView;

    private Rigidbody _rigidbody;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _characterFxSystem = GetComponent<CharacterFxSystem>();
        _healthView = GetComponent<HealthView>();

        _moveSystem =
            new CharacterMoveSystem(_rigidbody, _cameraService, _characterSettingsConfiguration, _characterFxSystem);

        _cameraSystem =
            new CharacterCameraMoveSystem(_cameraService.MainCamera, transform, _characterCameraSettingsConfiguration);

        CharacterPanel characterPanel = (CharacterPanel)_panelService.GetPanel(PanelName.CharacterPanel);
        _healthSystem = new HealthSystem(_characterSettingsConfiguration.StartHealth,
            new[] { characterPanel.CharacterHealthView, _healthView }
        );
        _healthSystem.Initialize();
    }

    private void Update()
    {
        _moveSystem.UpdateDash(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        _moveSystem.Move();
        _moveSystem.Rotate();
        _moveSystem.Jump();
    }

    private void LateUpdate()
    {
        _cameraSystem.Move();
    }

    private void OnCollisionEnter(Collision other)
    {
        var ground = other.gameObject.GetComponent<Ground>();
        if (ground != null)
            _moveSystem.ResetCanJump();
    }
}