using Core;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterMoveSystem
{
    private readonly Rigidbody _rigidbody;
    private readonly ICameraService _cameraService;
    private readonly CharacterStats _characterStats;
    private readonly CharacterFxSystem _characterFxSystem;
    private readonly GameObject _characterModel;
    private readonly CharacterAnimationSystem _characterAnimationSystem;
    private readonly PauseEntity _pauseEntity;

    private readonly InputSystem_Actions _inputActions = new();

    private Vector3 _direction;
    private Vector3 _currentVelocity;

    private readonly InputAction _dashAction;
    private float _dashCooldownTimer = 0f;

    private readonly float _dashForce = 25f;
    private readonly float _dashCooldown = 1f;
    private const float JUMP_INPUT_BUFFER_TIME = 0.15f;

    private bool _canMove = true;
    private bool _canJump = true;
    private bool _isGrounded = true;
    private float _jumpInputBufferTimer;

    public CharacterMoveSystem(Rigidbody rigidbody,
        ICameraService cameraService, CharacterStats characterStats,
        CharacterFxSystem characterFxSystem, GameObject characterModel,
        CharacterAnimationSystem characterAnimationSystem, PauseEntity pauseEntity)
    {
        _rigidbody = rigidbody;
        _cameraService = cameraService;

        _characterFxSystem = characterFxSystem;
        _characterModel = characterModel;
        _characterStats = characterStats;
        _characterAnimationSystem = characterAnimationSystem;
        _pauseEntity = pauseEntity;

        _dashAction = _inputActions.Player.Dash;
        _dashAction.started += OnDashStarted;

        _inputActions.Enable();
    }

    public void Move()
    {
        if (_canMove == false || _pauseEntity.IsPauseEntity)
            return;

        Vector2 input = _inputActions.Player.Move.ReadValue<Vector2>();

        Vector3 forward = _cameraService.MainCamera.transform.forward;
        Vector3 right = _cameraService.MainCamera.transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = Vector3.zero;

        var isMoving = input.magnitude > 0.1f;

        _characterAnimationSystem.MovementPlay(isMoving);

        if (isMoving)
        {
            moveDirection = (forward * input.y + right * input.x).normalized;
        }

        _characterAnimationSystem.GroundConditionState(_isGrounded);

        if (!_isGrounded)
        {
            ApplyEnhancedGravity();

            Vector3 currentHorizontal = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
            float currentSpeed = currentHorizontal.magnitude;

            if (currentSpeed > 0.01f && moveDirection != Vector3.zero)
            {
                float dot = Vector3.Dot(currentHorizontal.normalized, moveDirection);

                if (dot < 0f)
                {
                    float airBrakeForce = _characterStats.MovementAcceleration * 0.8f;
                    _rigidbody.AddForce(-currentHorizontal.normalized * airBrakeForce, ForceMode.Acceleration);
                }
                else
                {
                    float airControlSpeed = _characterStats.MovementSpeed * 1;
                    Vector3 desiredAirVelocity = moveDirection * airControlSpeed;
                    Vector3 velocityDiff = desiredAirVelocity - currentHorizontal;

                    float airAcceleration = _characterStats.MovementAcceleration * 1f;
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
            : _direction * _characterStats.MovementSpeed;

        Vector3 currentHorizontalVelocity = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
        Vector3 velocityDifference = desiredHorizontalVelocity - currentHorizontalVelocity;

        if (velocityDifference.magnitude > 0.01f)
        {
            _rigidbody.AddForce(velocityDifference * _characterStats.MovementAcceleration,
                ForceMode.Acceleration);
        }
    }

    public void CanMove(bool state) =>
        _canMove = state;

    public void SetGrounded(bool isGrounded)
    {
        if (isGrounded && !_isGrounded)
            _canJump = true;

        _isGrounded = isGrounded;
    }

    public void CaptureJumpInput(float deltaTime)
    {
        _jumpInputBufferTimer = Mathf.Max(0f, _jumpInputBufferTimer - deltaTime);

        if (_inputActions.Player.Jump.WasPressedThisFrame())
            _jumpInputBufferTimer = JUMP_INPUT_BUFFER_TIME;
    }

    public void Jump()
    {
        if (_jumpInputBufferTimer <= 0f || !_canJump)
            return;

        _jumpInputBufferTimer = 0f;
        _rigidbody.AddForce(Vector3.up * _characterStats.JumpForce, ForceMode.Force);
        _canJump = false;
        _isGrounded = false;
        _characterFxSystem.ActivateJump();
        _characterAnimationSystem.JumpPlay();
    }

    public void Rotate(float deltaTime)
    {
        if (_direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_direction);

            float rotationSpeed = _characterStats.RotationSpeed;
            _characterModel.transform.rotation = Quaternion.Slerp(
                _characterModel.transform.rotation,
                targetRotation,
                rotationSpeed * deltaTime
            );
        }
    }

    public void UpdateDash(float deltaTime)
    {
        if (_dashCooldownTimer > 0f)
            _dashCooldownTimer -= deltaTime;
    }

    private void ApplyEnhancedGravity()
    {
        float enhancedGravity = Physics.gravity.y * _characterStats.GravityMultiplier;

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
}
