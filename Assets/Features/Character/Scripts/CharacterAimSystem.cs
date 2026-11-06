using Core;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class CharacterAimSystem
{
    private readonly CharacterFacade _character;
    private readonly ICameraService _cameraService;
    private readonly PauseEntity _pauseEntity;
    private readonly CharacterPanel _panel;
    private Camera _camera;

    public bool IsAiming => _camera != null && _camera.isActiveAndEnabled &&
                            _character.isActiveAndEnabled &&
                            _character.HealthSystem?.IsDead == false &&
                            !_pauseEntity.IsPauseEntity &&
                            _panel != null && _panel.isActiveAndEnabled &&
                            Application.isFocused && Mouse.current?.rightButton.isPressed == true;

    public Vector3 Direction => _camera != null ? _camera.transform.forward : _character.CameraPivot.forward;

    public CharacterAimSystem(CharacterFacade character, ICameraService cameraService,
        PauseEntity pauseEntity, CharacterPanel panel)
    {
        _character = character;
        _cameraService = cameraService;
        _pauseEntity = pauseEntity;
        _panel = panel;
        Hide();
    }

    public void Tick()
    {
        CinemachineBrain brain = _cameraService.MainCamera != null
            ? CinemachineCore.FindPotentialTargetBrain(_cameraService.MainCamera)
            : null;
        _camera = brain != null ? brain.OutputCamera : Camera.main;

        if (_panel != null && _panel.AimCrosshair != null)
        {
            bool visible = IsAiming;
            if (_panel.AimCrosshair.activeSelf != visible)
                _panel.AimCrosshair.SetActive(visible);
        }
    }

    public void Hide()
    {
        if (_panel != null && _panel.AimCrosshair != null)
            _panel.AimCrosshair.SetActive(false);
    }
}
