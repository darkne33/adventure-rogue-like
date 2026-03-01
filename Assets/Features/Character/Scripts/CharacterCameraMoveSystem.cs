using UnityEngine;

public class CharacterCameraMoveSystem
{
    private readonly Transform _cameraPivot;
    private readonly CharacterCameraSettingsConfiguration _settings;
    private readonly InputSystem_Actions _inputActions;

    private float _yaw = 0f;
    private float _pitch = 10f;

    private readonly float _topClamp = 70f;
    private readonly float _bottomClamp = -30f;
    private readonly float _cameraAngleOverride = 0f;

    public CharacterCameraMoveSystem(
        Transform cameraPivot,
        CharacterCameraSettingsConfiguration settings)
    {
        _cameraPivot = cameraPivot;
        _settings = settings;

        _inputActions = new InputSystem_Actions();
        _inputActions.Enable();

        _yaw = _cameraPivot.eulerAngles.y;
        _pitch = Mathf.Clamp(_cameraPivot.eulerAngles.x, _bottomClamp, _topClamp);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Move()
    {
        if (_cameraPivot == null) return;

        Vector2 lookInput = _inputActions.Player.Look.ReadValue<Vector2>();
        const float threshold = 0.01f;

        if (lookInput.sqrMagnitude >= threshold)
        {
            bool isMouse = true;
            float deltaTimeMultiplier = isMouse ? 1.0f : Time.deltaTime;

            _yaw += lookInput.x * _settings.MouseSensitivity * deltaTimeMultiplier;
            _pitch -= lookInput.y * _settings.MouseSensitivity * deltaTimeMultiplier;
        }

        _pitch = ClampAngle(_pitch, _bottomClamp, _topClamp);
        _yaw = ClampAngle(_yaw, float.MinValue, float.MaxValue);

        _cameraPivot.rotation = Quaternion.Euler(
            _pitch + _cameraAngleOverride,
            _yaw,
            0f
        );
    }

    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f) angle += 360f;
        if (angle > 360f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }
}