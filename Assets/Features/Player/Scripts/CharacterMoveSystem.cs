using Core;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterMoveSystem
{
    private readonly Rigidbody _rigidbody;
    private readonly InputAction _moveAction;
    private readonly ICameraService _cameraService;
    private readonly CharacterSettingsConfiguration _characterSettingsConfiguration;
 

    private Vector3 _direction;
    private Vector3 _currentVelocity;
    
    public CharacterMoveSystem(Rigidbody rigidbody, PlayerInput playerInput,
        ICameraService cameraService, CharacterSettingsConfiguration characterSettingsConfiguration)
    {
        _rigidbody = rigidbody;
        _cameraService = cameraService;
        _characterSettingsConfiguration = characterSettingsConfiguration;
  

        _moveAction = playerInput.actions["Move"];
    }

    public void Move()
    {
        Vector2 input = _moveAction.ReadValue<Vector2>();
        
        Vector3 forward = _cameraService.MainCamera.transform.forward;
        Vector3 right = _cameraService.MainCamera.transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        _direction = (forward * input.y + right * input.x).normalized;

        Vector3 targetVelocity = _direction * _characterSettingsConfiguration.MoveSpeed;

        if (input.magnitude > 0.1f)
        {
            _currentVelocity = Vector3.Lerp(_currentVelocity, targetVelocity,
                _characterSettingsConfiguration.Acceleration * Time.fixedDeltaTime);
        }
        else
        {
            _currentVelocity = Vector3.Lerp(_currentVelocity, Vector3.zero,
                _characterSettingsConfiguration.Deceleration * Time.fixedDeltaTime);
        }

        _rigidbody.linearVelocity = new Vector3(_currentVelocity.x, _rigidbody.linearVelocity.y, _currentVelocity.z);
    }

    public void Rotate()
    {
        if (_direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_direction, Vector3.up);
            _rigidbody.rotation = Quaternion.Slerp(_rigidbody.rotation, targetRotation,
                _characterSettingsConfiguration.RotationSpeed * Time.fixedDeltaTime);
        }
    }
}