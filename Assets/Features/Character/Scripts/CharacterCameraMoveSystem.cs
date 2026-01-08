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
    
    public CharacterCameraMoveSystem(Camera camera, Transform target,
        CharacterCameraSettingsConfiguration characterCameraSettingsConfiguration)
    {
        _camera = camera;
        _target = target;
        _characterCameraSettingsConfiguration = characterCameraSettingsConfiguration;

        _inputActions = new InputSystem_Actions();

        if (target == null)
        {
            Debug.LogError("ThirdPersonCamera: Target is not assigned!");
            return;
        }
        
        _yaw = _camera.transform.eulerAngles.y;
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (target == null)
        {
            Debug.LogError("ThirdPersonCamera: Target is not assigned!");
            return;
        }

        _yaw = _camera.transform.eulerAngles.y;

        _inputActions.Enable();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    public void Move()
    {
        if (_target == null) 
            return;
        
        Vector2 mouseDelta = _inputActions.Player.Look.ReadValue<Vector2>();

        _yaw += mouseDelta.x * _characterCameraSettingsConfiguration.MouseSensitivity;
        _pitch -= mouseDelta.y * _characterCameraSettingsConfiguration.MouseSensitivity;
        
        _pitch = Mathf.Clamp(_pitch, _characterCameraSettingsConfiguration.MinVerticalAngle, _characterCameraSettingsConfiguration.MaxVerticalAngle);

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        Vector3 cameraOffset = new Vector3(0f, _characterCameraSettingsConfiguration.Height, -_characterCameraSettingsConfiguration.DistanceToTarget);
        Vector3 desiredPosition = _target.position + rotation * cameraOffset;

        _camera.transform.position = desiredPosition;

        Vector3 lookAtPoint = _target.position + Vector3.up * (_characterCameraSettingsConfiguration.Height * 0.5f);
        _camera.transform.LookAt(lookAtPoint);
    }
}