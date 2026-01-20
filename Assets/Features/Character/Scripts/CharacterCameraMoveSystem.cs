using UnityEngine;
using UnityEngine.InputSystem;

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

        // === 1. Обработка ввода мыши ===
        Vector2 mouseDelta = _inputActions.Player.Look.ReadValue<Vector2>();

        _yaw += mouseDelta.x * _characterCameraSettingsConfiguration.MouseSensitivity;
        _pitch -= mouseDelta.y * _characterCameraSettingsConfiguration.MouseSensitivity;
        _pitch = Mathf.Clamp(_pitch,
            _characterCameraSettingsConfiguration.MinVerticalAngle,
            _characterCameraSettingsConfiguration.MaxVerticalAngle);

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        // === 2. Желаемая позиция камеры ===
        Vector3 cameraOffset = new Vector3(
            0f,
            _characterCameraSettingsConfiguration.Height,
            -_characterCameraSettingsConfiguration.DistanceToTarget
        );
        Vector3 desiredPosition = _target.position + rotation * cameraOffset;

        // === 3. Проверка на столкновения (RAYCAST ОТ ИГРОКА К КАМЕРЕ) ===
        Vector3 directionToCamera = desiredPosition - _target.position;
        float distanceToCamera = directionToCamera.magnitude;

        // Минимальное расстояние, чтобы камера не сливалась с игроком
        float minSafeDistance = Mathf.Max(_characterCameraSettingsConfiguration.MinDistanceToTarget, 0.1f);

        // Проверяем, нет ли препятствий между игроком и камерой
        if (Physics.Raycast(
                _target.position,
                directionToCamera.normalized,
                out RaycastHit hit,
                distanceToCamera,
                _characterCameraSettingsConfiguration.CameraCollisionLayers))
        {
            // Если есть препятствие — ставим камеру чуть ближе к игроку
            float safeDistance = Mathf.Max(hit.distance - 0.1f, minSafeDistance);
            desiredPosition = _target.position + directionToCamera.normalized * safeDistance;
        }
        else
        {
            // Если нет препятствий, но расстояние меньше минимума — корректируем
            if (distanceToCamera < minSafeDistance)
            {
                desiredPosition = _target.position + directionToCamera.normalized * minSafeDistance;
            }
        }

        // === 4. Применяем позицию и поворот ===
        _camera.transform.position = desiredPosition;

        Vector3 lookAtPoint = _target.position + Vector3.up * (_characterCameraSettingsConfiguration.Height * 0.5f);
        _camera.transform.LookAt(lookAtPoint);
    }
}