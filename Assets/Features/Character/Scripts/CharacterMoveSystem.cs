using Core;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterMoveSystem
{
    private readonly Rigidbody _rigidbody;
    private readonly ICameraService _cameraService;
    private readonly CharacterSettingsConfiguration _characterSettingsConfiguration;
    private readonly CharacterFxSystem _characterFxSystem;
    private readonly GameObject _characterModel;

    private readonly InputSystem_Actions _inputActions = new();

    private Vector3 _direction;
    private Vector3 _currentVelocity;

    private bool _canJump = true;

    private readonly InputAction _dashAction;
    private float _dashCooldownTimer = 0f;

    private readonly float _dashForce = 25f;
    private readonly float _dashCooldown = 1f;

    private bool _canMove = true;

    public CharacterMoveSystem(Rigidbody rigidbody,
        ICameraService cameraService, CharacterSettingsConfiguration characterSettingsConfiguration,
        CharacterFxSystem characterFxSystem, GameObject characterModel)
    {
        _rigidbody = rigidbody;
        _cameraService = cameraService;
        _characterSettingsConfiguration = characterSettingsConfiguration;
        _characterFxSystem = characterFxSystem;
        _characterModel = characterModel;

        _dashAction = _inputActions.Player.Dash;
        _dashAction.started += OnDashStarted;

        _inputActions.Enable();
    }

    public void Move()
    {
        if (_canMove == false)
            return;

        Vector2 input = _inputActions.Player.Move.ReadValue<Vector2>();

        Vector3 forward = _cameraService.MainCamera.transform.forward;
        Vector3 right = _cameraService.MainCamera.transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = Vector3.zero;
        if (input.magnitude > 0.1f)
        {
            moveDirection = (forward * input.y + right * input.x).normalized;
        }

        bool isGrounded = _canJump;

        _characterFxSystem.ActivateMovementTrail(isGrounded);

        if (!isGrounded)
        {
            ApplyEnhancedGravity();

            Vector3 currentHorizontal = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
            float currentSpeed = currentHorizontal.magnitude;

            if (currentSpeed > 0.01f && moveDirection != Vector3.zero)
            {
                float dot = Vector3.Dot(currentHorizontal.normalized, moveDirection);

                if (dot < 0f)
                {
                    float airBrakeForce = _characterSettingsConfiguration.Acceleration * 0.8f;
                    _rigidbody.AddForce(-currentHorizontal.normalized * airBrakeForce, ForceMode.Acceleration);
                }
                else
                {
                    float airControlSpeed = _characterSettingsConfiguration.MoveSpeed * 1;
                    Vector3 desiredAirVelocity = moveDirection * airControlSpeed;
                    Vector3 velocityDiff = desiredAirVelocity - currentHorizontal;

                    float airAcceleration = _characterSettingsConfiguration.Acceleration * 1f;
                    if (velocityDiff.magnitude > 0.01f)
                    {
                        _rigidbody.AddForce(velocityDiff * airAcceleration, ForceMode.Acceleration);
                    }
                }
            }

            _direction = moveDirection;

            return;
        }

        _direction = moveDirection;

        bool blocked = false;
        if (input.magnitude > 0.1f && Physics.Raycast(_rigidbody.transform.position, _direction, out var hit, 1f))
        {
            if (hit.collider.GetComponent<Obstacle>() != null)
                blocked = true;
        }

        Vector3 desiredHorizontalVelocity = blocked
            ? Vector3.zero
            : _direction * _characterSettingsConfiguration.MoveSpeed;

        Vector3 currentHorizontalVelocity = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
        Vector3 velocityDifference = desiredHorizontalVelocity - currentHorizontalVelocity;

        if (velocityDifference.magnitude > 0.01f)
        {
            _rigidbody.AddForce(velocityDifference * _characterSettingsConfiguration.Acceleration,
                ForceMode.Acceleration);
        }
    }

    public void CanMove(bool state) =>
        _canMove = state;

    public void Jump()
    {
        if (_inputActions.Player.Jump.triggered && _canJump)
        {
            _rigidbody.AddForce(Vector3.up * _characterSettingsConfiguration.JumpForce, ForceMode.Force);
            _canJump = false;
            _characterFxSystem.ActivateJump();
        }
    }

    public void ResetCanJump() =>
        _canJump = true;

    public void Rotate()
    {
        if (_direction.magnitude > 0.1f)
            _characterModel.transform.rotation = Quaternion.LookRotation(_direction);
    }

    private void ApplyEnhancedGravity()
    {
        float enhancedGravity = Physics.gravity.y * _characterSettingsConfiguration.GravityMultiplier;

        Vector3 currentVelocity = _rigidbody.linearVelocity;

        float gravityDelta = (enhancedGravity - Physics.gravity.y) * Time.fixedDeltaTime;
        currentVelocity.y += gravityDelta;

        _rigidbody.linearVelocity = currentVelocity;
    }

    private void OnDashStarted(InputAction.CallbackContext context) =>
        TryDash();

    private void TryDash()
    {
        if (_dashCooldownTimer > 0f || _direction == Vector3.zero)
            return;

        _characterFxSystem.ActivateDash();
        Vector3 dashImpulse = _direction * _dashForce;
        _rigidbody.AddForce(dashImpulse, ForceMode.Impulse);

        _dashCooldownTimer = _dashCooldown;
    }

    public void UpdateDash(float deltaTime)
    {
        if (_dashCooldownTimer > 0f)
            _dashCooldownTimer -= deltaTime;
    }
}