using UnityEngine;

public class CharacterCameraMoveSystem
{
    private readonly Camera _camera;
    private readonly Transform _target;
    private readonly CharacterCameraSettingsConfiguration _characterCameraSettingsConfiguration;
    private readonly InputSystem_Actions _inputActions;

    private float _yaw = 0f;
    private float _pitch = 10f;

    public CharacterCameraMoveSystem(
        Camera camera,
        Transform target,
        CharacterCameraSettingsConfiguration characterCameraSettingsConfiguration)
    {
        _camera = camera;
        _target = target;
        _characterCameraSettingsConfiguration = characterCameraSettingsConfiguration;

        if (_target == null)
        {
            Debug.LogError("CharacterCameraMoveSystem: Target is not assigned!");
            return;
        }

        _inputActions = new InputSystem_Actions();
        _inputActions.Enable();

        _yaw = _camera.transform.eulerAngles.y;
        _pitch = Mathf.Clamp(_camera.transform.eulerAngles.x,
            _characterCameraSettingsConfiguration.MinVerticalAngle,
            _characterCameraSettingsConfiguration.MaxVerticalAngle);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Move()
    {
        if (_target == null || _camera == null) return;

        Vector2 mouseDelta = _inputActions.Player.Look.ReadValue<Vector2>();
        _yaw += mouseDelta.x * _characterCameraSettingsConfiguration.MouseSensitivity;
        _pitch -= mouseDelta.y * _characterCameraSettingsConfiguration.MouseSensitivity;

        _pitch = Mathf.Clamp(_pitch,
            _characterCameraSettingsConfiguration.MinVerticalAngle,
            _characterCameraSettingsConfiguration.MaxVerticalAngle);

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 desiredPosition = _target.position + rotation * new Vector3(
            0f,
            _characterCameraSettingsConfiguration.Height,
            -_characterCameraSettingsConfiguration.DistanceToTarget
        );

        Vector3 dir = desiredPosition - _target.position;
        float rayLength = dir.magnitude + 0.3f;

        if (Physics.Raycast(_target.position, dir.normalized, out RaycastHit hit, rayLength,
                _characterCameraSettingsConfiguration.CameraCollisionLayers))
        {
            float safeDistance = Mathf.Max(hit.distance - 0.15f,
                _characterCameraSettingsConfiguration.MinDistanceToTarget);
            desiredPosition = _target.position + dir.normalized * safeDistance;

            desiredPosition.y = _target.position.y + _characterCameraSettingsConfiguration.Height;
        }

        _camera.transform.position = desiredPosition;
        _camera.transform.LookAt(_target.position + Vector3.up * _characterCameraSettingsConfiguration.Height * 0.5f);
    }
}