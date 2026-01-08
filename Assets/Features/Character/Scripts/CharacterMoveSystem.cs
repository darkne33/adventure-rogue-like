using Core;
using UnityEngine;

public class CharacterMoveSystem
{
    private readonly Rigidbody _rigidbody;
    private readonly ICameraService _cameraService;
    private readonly CharacterSettingsConfiguration _characterSettingsConfiguration;
    private readonly InputSystem_Actions _inputActions = new();

    private Vector3 _direction;
    private Vector3 _currentVelocity;

    private bool _canJump = true;
    
    public CharacterMoveSystem(Rigidbody rigidbody,
        ICameraService cameraService, CharacterSettingsConfiguration characterSettingsConfiguration)
    {
        _rigidbody = rigidbody;
        _cameraService = cameraService;
        _characterSettingsConfiguration = characterSettingsConfiguration;
        _inputActions.Enable();
    }

    public void Move()
    {
        Vector2 input = _inputActions.Player.Move.ReadValue<Vector2>();
        
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

    public void Jump()
    {
        if (_inputActions.Player.Jump.triggered && _canJump)
        {
            _rigidbody.AddForce(Vector3.up * _characterSettingsConfiguration.JumpForce, ForceMode.Force);
            _canJump = false;
        }
    }

    public void ResetCanJump() 
        => _canJump = true;

    public void Rotate()
    {
        if (_direction.magnitude > 0.1f) 
            _rigidbody.transform.rotation = Quaternion.LookRotation(_direction);
    }
}