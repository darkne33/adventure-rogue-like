using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterCameraMoveSystem
{
    private readonly Transform _cameraPivot;
    private readonly CharacterCameraSettingsConfiguration _settings;
    private readonly InputSystem_Actions _inputActions;
    private readonly PauseEntity _pauseEntity;

    private float _yaw;
    private float _pitch;

    private float _yawVelocity;
    private float _pitchVelocity;

    private readonly float _topClamp = 70f;
    private readonly float _bottomClamp = -30f;

    private const float ACCELERATION = 30f; // насколько быстро разгоняется
    private const float FRICTION = 8f; // затухание (чем больше — тем быстрее стоп)
    private const float MAX_SPEED = 300f; // ограничение скорости
    private const float INPUT_THRESHOLD = 0.01f;

    public CharacterCameraMoveSystem(
        Transform cameraPivot,
        CharacterCameraSettingsConfiguration settings, PauseEntity pauseEntity)
    {
        _cameraPivot = cameraPivot;
        _settings = settings;
        _pauseEntity = pauseEntity;

        _inputActions = new InputSystem_Actions();
        _inputActions.Enable();

        _yaw = _cameraPivot.eulerAngles.y;
        _pitch = NormalizeAngle(_cameraPivot.eulerAngles.x);
    }

    public void Move()
    {
        if (_cameraPivot == null || _pauseEntity.IsPauseEntity) return;

        Vector2 lookInput = _inputActions.Player.Look.ReadValue<Vector2>();

        bool isMouse = IsMouseInput();
        float dt = isMouse ? 1f : Time.deltaTime;

        if (lookInput.sqrMagnitude >= INPUT_THRESHOLD)
        {
            float sensitivity = _settings.MouseSensitivity;

            _yawVelocity += lookInput.x * sensitivity * ACCELERATION * dt;
            _pitchVelocity -= lookInput.y * sensitivity * ACCELERATION * dt;
        }

        _yawVelocity = Mathf.Clamp(_yawVelocity, -MAX_SPEED, MAX_SPEED);
        _pitchVelocity = Mathf.Clamp(_pitchVelocity, -MAX_SPEED, MAX_SPEED);

        _yaw += _yawVelocity * Time.deltaTime;
        _pitch += _pitchVelocity * Time.deltaTime;

        _yawVelocity = Mathf.Lerp(_yawVelocity, 0f, FRICTION * Time.deltaTime);
        _pitchVelocity = Mathf.Lerp(_pitchVelocity, 0f, FRICTION * Time.deltaTime);

        _pitch = Mathf.Clamp(_pitch, _bottomClamp, _topClamp);

        _cameraPivot.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    private bool IsMouseInput()
    {
        var control = _inputActions.Player.Look.activeControl;
        if (control == null) return true;

        return control.device is Mouse;
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        return angle;
    }
}