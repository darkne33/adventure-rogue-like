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
    private readonly CharacterCameraMoveSystem _cameraMoveSystem;
    private readonly PauseEntity _pauseEntity;

    private readonly InputSystem_Actions _inputActions = new();

    private Vector3 _direction;
    private Vector3 _currentVelocity;

    private readonly InputAction _dashAction;
    private float _dashCooldownTimer = 0f;

    private readonly float _dashForce = 25f;
    private readonly float _dashCooldown = 1f;
    private const float JUMP_INPUT_BUFFER_TIME = 0.15f;
    private const float MinBunnyHopHorizontalSpeed = 0.5f;
    private const float ReverseLandingSlideCarryMultiplier = 0.4f;

    private bool _canMove = true;
    private bool _canJump = true;
    private bool _isGrounded = true;
    private float _jumpInputBufferTimer;
    private float _coyoteTimer;
    private float _bunnyHopResetTimer;
    private float _bunnyHopSpeedBonus;
    private float _landingSlideTimer;
    private float _landingSlideSpeed;
    private float _landingSlideTargetSpeed;
    private float _jumpInertiaTimer;
    private float _lastCameraYaw;
    private bool _hasLastCameraYaw;
    private bool _hasJumpedSinceGrounded;
    private Vector3 _landingSlideDirection;
    private Vector3 _jumpInertiaVelocity;

    public CharacterMoveSystem(Rigidbody rigidbody,
        ICameraService cameraService, CharacterStats characterStats,
        CharacterFxSystem characterFxSystem, GameObject characterModel,
        CharacterAnimationSystem characterAnimationSystem,
        CharacterCameraMoveSystem cameraMoveSystem, PauseEntity pauseEntity)
    {
        _rigidbody = rigidbody;
        _cameraService = cameraService;

        _characterFxSystem = characterFxSystem;
        _characterModel = characterModel;
        _characterStats = characterStats;
        _characterAnimationSystem = characterAnimationSystem;
        _cameraMoveSystem = cameraMoveSystem;
        _pauseEntity = pauseEntity;

        _dashAction = _inputActions.Player.Dash;
        _dashAction.started += OnDashStarted;

        _inputActions.Enable();
    }

    public void Move()
    {
        if (_canMove == false || _pauseEntity.IsPauseEntity)
            return;

        UpdateCoyoteTimer();
        UpdateBunnyHopResetTimer();
        UpdateLandingSlide();
        UpdateJumpInertia();

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
            UpdateControlledBunnyHop(currentHorizontal, currentSpeed);

            if (currentSpeed > 0.01f)
            {
                if (moveDirection == Vector3.zero)
                {
                    if (IsJumpInertiaActive())
                        ApplyJumpInertiaAirControl(currentHorizontal, Vector3.zero);
                }
                else
                {
                    ApplyAirSteering(currentHorizontal, moveDirection, currentSpeed);
                }
            }
            else if (moveDirection != Vector3.zero)
            {
                Vector3 desiredAirVelocity = moveDirection * GetMovementSpeed();
                ApplyJumpInertiaAirControl(Vector3.zero, desiredAirVelocity);
            }

            _direction = moveDirection;

            return;
        }

        _direction = moveDirection;

        bool blocked = false;
        if (input.magnitude > 0.1f && Physics.Raycast(_rigidbody.transform.position, _direction, out var hit, 1f))
        {
            if (hit.collider.GetComponent<Wall>() != null ||
                hit.collider.GetComponent<Obstacle>() != null)
            {
                blocked = true;
                ResetBunnyHopBonus();
            }
        }

        Vector3 desiredHorizontalVelocity = blocked
            ? Vector3.zero
            : _direction * GetMovementSpeed();
        desiredHorizontalVelocity = ApplyLandingSlideVelocity(desiredHorizontalVelocity);

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
        {
            _canJump = true;
            if (_hasJumpedSinceGrounded)
            {
                _cameraMoveSystem.PlayLandingShake();
                StartLandingSlide();
            }

            ResetJumpInertia();
            _hasJumpedSinceGrounded = false;
            StartBunnyHopResetTimer();
        }

        _isGrounded = isGrounded;

        if (_isGrounded)
            _coyoteTimer = Mathf.Max(0f, _characterStats.CoyoteTime);
    }

    public void ResetBunnyHopBonus()
    {
        ResetBunnyHopState();
        ResetLandingSlide();
        ResetJumpInertia();
        _hasJumpedSinceGrounded = false;
    }

    public void CaptureJumpInput(float deltaTime)
    {
        if (_pauseEntity.IsPauseEntity)
        {
            _jumpInputBufferTimer = 0f;
            return;
        }

        _jumpInputBufferTimer = Mathf.Max(0f, _jumpInputBufferTimer - deltaTime);

        if (_inputActions.Player.Jump.WasPressedThisFrame())
            _jumpInputBufferTimer = JUMP_INPUT_BUFFER_TIME;
    }

    public void Jump()
    {
        bool canUseCoyoteTime = _isGrounded || _coyoteTimer > 0f;
        if (_pauseEntity.IsPauseEntity || _jumpInputBufferTimer <= 0f || !_canJump || !canUseCoyoteTime)
            return;

        _jumpInputBufferTimer = 0f;
        _cameraMoveSystem.StopLandingShake();
        _rigidbody.AddForce(Vector3.up * _characterStats.JumpForce, ForceMode.Force);
        ApplyJumpForwardImpulse();
        StartJumpInertia();
        _canJump = false;
        _isGrounded = false;
        _coyoteTimer = 0f;
        _bunnyHopResetTimer = 0f;
        _hasJumpedSinceGrounded = true;
        IncreaseBunnyHopBonus();
        _characterFxSystem.ActivateJump();
        _characterAnimationSystem.JumpPlay();
    }

    public void Rotate(float deltaTime)
    {
        if (_pauseEntity.IsPauseEntity)
            return;

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
        if (_pauseEntity.IsPauseEntity)
            return;

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

    private void UpdateCoyoteTimer()
    {
        if (_isGrounded)
            return;

        _coyoteTimer = Mathf.Max(0f, _coyoteTimer - Time.fixedDeltaTime);
    }

    private void StartBunnyHopResetTimer()
    {
        if (_bunnyHopSpeedBonus <= 0f)
            return;

        _bunnyHopResetTimer = Mathf.Max(0f, _characterStats.BunnyHopResetDelay);
    }

    private void UpdateBunnyHopResetTimer()
    {
        if (!_isGrounded || _bunnyHopResetTimer <= 0f)
            return;

        _bunnyHopResetTimer = Mathf.Max(0f, _bunnyHopResetTimer - Time.fixedDeltaTime);
        if (_bunnyHopResetTimer <= 0f)
            ResetBunnyHopBonus();
    }

    private void StartLandingSlide()
    {
        float duration = Mathf.Max(0f, _characterStats.LandingSlideDuration);
        if (duration <= 0f)
            return;

        Vector3 horizontalVelocity =
            new(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
        float speed = horizontalVelocity.magnitude;
        if (speed < MinBunnyHopHorizontalSpeed)
            return;

        _landingSlideTimer = duration;
        _landingSlideSpeed = speed * Mathf.Max(1f, _characterStats.LandingSlideSpeedMultiplier);
        _landingSlideTargetSpeed = 0f;
        _landingSlideDirection = horizontalVelocity.normalized;
        ApplyLandingSlideStartVelocity();
    }

    private void UpdateLandingSlide()
    {
        if (_landingSlideTimer <= 0f)
            return;

        _landingSlideTimer = Mathf.Max(0f, _landingSlideTimer - Time.fixedDeltaTime);
        _landingSlideSpeed = Mathf.MoveTowards(
            _landingSlideSpeed,
            _landingSlideTargetSpeed,
            Mathf.Max(0f, _characterStats.LandingSlideDeceleration) * Time.fixedDeltaTime);

        if (_landingSlideTimer <= 0f)
        {
            ResetLandingSlide();
        }
    }

    private Vector3 ApplyLandingSlideVelocity(Vector3 desiredHorizontalVelocity)
    {
        if (_landingSlideTimer <= 0f || _landingSlideDirection == Vector3.zero)
            return desiredHorizontalVelocity;

        Vector3 slideVelocity = _landingSlideDirection * _landingSlideSpeed;
        if (desiredHorizontalVelocity.sqrMagnitude <= 0.01f)
            return slideVelocity;

        float inputCarry = Mathf.Clamp01(_characterStats.LandingSlideInputCarry);
        float inputDot = Vector3.Dot(desiredHorizontalVelocity.normalized, _landingSlideDirection);
        if (inputDot < 0f)
            inputCarry *= ReverseLandingSlideCarryMultiplier;

        return desiredHorizontalVelocity + slideVelocity * inputCarry;
    }

    private void ApplyLandingSlideStartVelocity()
    {
        Vector3 currentHorizontalVelocity = new(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
        Vector3 targetHorizontalVelocity = _landingSlideDirection * _landingSlideSpeed;
        Vector3 velocityChange = targetHorizontalVelocity - currentHorizontalVelocity;

        if (velocityChange.sqrMagnitude <= 0.0001f)
            return;

        _rigidbody.AddForce(velocityChange, ForceMode.VelocityChange);
    }

    private float GetMovementSpeed() =>
        _characterStats.MovementSpeed * (1f + _bunnyHopSpeedBonus);

    private void IncreaseBunnyHopBonus()
    {
        float maxBonus = Mathf.Max(0f, _characterStats.MaxBunnyHopSpeedBonus);
        float bonusPerJump = Mathf.Max(0f, _characterStats.BunnyHopSpeedBonusPerJump);
        if (maxBonus <= 0f || bonusPerJump <= 0f)
            return;

        Vector3 horizontalVelocity = new(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
        if (CanContinueBunnyHop(horizontalVelocity) == false)
        {
            ResetBunnyHopState();
            return;
        }

        _bunnyHopSpeedBonus = Mathf.Min(maxBonus, _bunnyHopSpeedBonus + bonusPerJump);
    }

    private void ApplyJumpForwardImpulse()
    {
        float impulse = Mathf.Max(0f, _characterStats.JumpForwardImpulse);
        if (impulse <= 0f)
            return;

        Vector3 impulseDirection = GetJumpForwardImpulseDirection();
        if (impulseDirection == Vector3.zero)
            return;

        _rigidbody.AddForce(impulseDirection * impulse, ForceMode.VelocityChange);
    }

    private void StartJumpInertia()
    {
        float duration = Mathf.Max(0f, _characterStats.JumpInertiaDuration);
        if (duration <= 0f)
        {
            ResetJumpInertia();
            return;
        }

        Vector3 horizontalVelocity = new(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
        if (horizontalVelocity.magnitude < MinBunnyHopHorizontalSpeed && _direction.sqrMagnitude > 0.01f)
            horizontalVelocity = _direction.normalized * GetMovementSpeed();

        if (horizontalVelocity.magnitude < MinBunnyHopHorizontalSpeed)
        {
            ResetJumpInertia();
            return;
        }

        _jumpInertiaTimer = duration;
        _jumpInertiaVelocity = horizontalVelocity;
    }

    private void UpdateJumpInertia()
    {
        if (_jumpInertiaTimer <= 0f)
            return;

        _jumpInertiaTimer = Mathf.Max(0f, _jumpInertiaTimer - Time.fixedDeltaTime);
        if (_jumpInertiaTimer <= 0f)
            ResetJumpInertia();
    }

    private void ApplyJumpInertiaAirControl(Vector3 currentHorizontalVelocity, Vector3 desiredAirVelocity)
    {
        Vector3 targetVelocity = ApplyJumpInertiaToDesiredVelocity(desiredAirVelocity);
        Vector3 velocityDiff = targetVelocity - currentHorizontalVelocity;

        float airAcceleration = Mathf.Max(0f, _characterStats.DefaultAirAcceleration);
        if (velocityDiff.magnitude > 0.01f)
            _rigidbody.AddForce(velocityDiff * airAcceleration, ForceMode.Acceleration);
    }

    private void ApplyAirSteering(
        Vector3 currentHorizontalVelocity, Vector3 moveDirection, float currentSpeed)
    {
        Vector3 currentDirection = currentHorizontalVelocity.normalized;
        float turnRadians = Mathf.Max(0f, _characterStats.AirTurnSpeed) *
                            Mathf.Deg2Rad * Time.fixedDeltaTime;
        Vector3 steeredDirection = Vector3.RotateTowards(
            currentDirection,
            moveDirection,
            turnRadians,
            0f);

        float targetSpeed = Mathf.Max(currentSpeed, GetMovementSpeed());
        Vector3 desiredAirVelocity = steeredDirection.normalized * targetSpeed;
        ApplyJumpInertiaAirControl(currentHorizontalVelocity, desiredAirVelocity);
    }

    private Vector3 ApplyJumpInertiaToDesiredVelocity(Vector3 desiredAirVelocity)
    {
        if (IsJumpInertiaActive() == false)
            return desiredAirVelocity;

        if (desiredAirVelocity.sqrMagnitude <= 0.01f)
            return _jumpInertiaVelocity;

        float duration = Mathf.Max(Time.fixedDeltaTime, _characterStats.JumpInertiaDuration);
        float progress = 1f - Mathf.Clamp01(_jumpInertiaTimer / duration);
        float airControl = Mathf.Lerp(
            Mathf.Clamp01(_characterStats.JumpInertiaAirControl),
            1f,
            progress);

        return Vector3.Lerp(_jumpInertiaVelocity, desiredAirVelocity, airControl);
    }

    private bool IsJumpInertiaActive() =>
        _jumpInertiaTimer > 0f && _jumpInertiaVelocity.sqrMagnitude > 0.01f;

    private Vector3 GetJumpForwardImpulseDirection()
    {
        if (_direction.sqrMagnitude > 0.01f)
            return _direction.normalized;

        Vector3 horizontalVelocity = new(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
        if (horizontalVelocity.sqrMagnitude > 0.01f)
            return horizontalVelocity.normalized;

        return GetCameraForward();
    }

    private void UpdateControlledBunnyHop(Vector3 horizontalVelocity, float currentSpeed)
    {
        if (_bunnyHopSpeedBonus <= 0f)
        {
            UpdateCameraYawSnapshot();
            return;
        }

        if (CanContinueBunnyHop(horizontalVelocity) == false)
        {
            ResetBunnyHopState();
            ApplyAirDeceleration(horizontalVelocity, _characterStats.DefaultAirDeceleration);
            UpdateCameraYawSnapshot();
            return;
        }

        ApplyCameraTurnSlowdown(horizontalVelocity, currentSpeed);
    }

    private bool CanContinueBunnyHop(Vector3 horizontalVelocity)
    {
        if (horizontalVelocity.magnitude < MinBunnyHopHorizontalSpeed)
            return false;

        Vector3 bunnyHopDirection = horizontalVelocity.normalized;
        Vector3 cameraForward = GetCameraForward();

        return Vector3.Dot(bunnyHopDirection, cameraForward) >= _characterStats.BunnyHopCameraAlignment;
    }

    private Vector3 GetCameraForward()
    {
        Vector3 cameraForward = _cameraService.MainCamera.transform.forward;
        cameraForward.y = 0f;
        return cameraForward.sqrMagnitude > 0.01f ? cameraForward.normalized : Vector3.forward;
    }

    private void ApplyCameraTurnSlowdown(Vector3 horizontalVelocity, float currentSpeed)
    {
        float currentYaw = _cameraService.MainCamera.transform.eulerAngles.y;
        if (_hasLastCameraYaw == false)
        {
            _lastCameraYaw = currentYaw;
            _hasLastCameraYaw = true;
            return;
        }

        float yawDelta = Mathf.Abs(Mathf.DeltaAngle(_lastCameraYaw, currentYaw));
        _lastCameraYaw = currentYaw;

        float slowdownSpeed = Mathf.Max(0f, _characterStats.BunnyHopCameraTurnSlowdownSpeed);
        if (slowdownSpeed <= 0f)
            return;

        float cameraTurnSpeed = yawDelta / Mathf.Max(Time.fixedDeltaTime, Mathf.Epsilon);
        if (cameraTurnSpeed <= slowdownSpeed)
            return;

        float penalty = Mathf.InverseLerp(slowdownSpeed, slowdownSpeed * 2f, cameraTurnSpeed);
        float strength = Mathf.Max(0f, _characterStats.BunnyHopCameraTurnSlowdownStrength);
        float slowdown = penalty * strength * Time.fixedDeltaTime;
        _bunnyHopSpeedBonus = Mathf.Max(0f, _bunnyHopSpeedBonus - slowdown);

        if (horizontalVelocity.sqrMagnitude <= 0.01f || currentSpeed <= _characterStats.MovementSpeed)
            return;

        Vector3 reducedHorizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            horizontalVelocity.normalized * _characterStats.MovementSpeed,
            currentSpeed * slowdown);
        _rigidbody.linearVelocity = new Vector3(
            reducedHorizontalVelocity.x,
            _rigidbody.linearVelocity.y,
            reducedHorizontalVelocity.z);
    }

    private void UpdateCameraYawSnapshot()
    {
        _lastCameraYaw = _cameraService.MainCamera.transform.eulerAngles.y;
        _hasLastCameraYaw = true;
    }

    private void ApplyAirDeceleration(Vector3 horizontalVelocity, float deceleration)
    {
        if (horizontalVelocity.sqrMagnitude <= 0.01f)
            return;

        _rigidbody.AddForce(
            -horizontalVelocity.normalized * Mathf.Max(0f, deceleration),
            ForceMode.Acceleration);
    }

    private void ResetBunnyHopState()
    {
        _bunnyHopSpeedBonus = 0f;
        _bunnyHopResetTimer = 0f;
    }

    private void ResetLandingSlide()
    {
        _landingSlideTimer = 0f;
        _landingSlideSpeed = 0f;
        _landingSlideTargetSpeed = 0f;
        _landingSlideDirection = Vector3.zero;
    }

    private void ResetJumpInertia()
    {
        _jumpInertiaTimer = 0f;
        _jumpInertiaVelocity = Vector3.zero;
    }

    private void OnDashStarted(InputAction.CallbackContext context) =>
        TryDash();

    private void TryDash()
    {
        if (_pauseEntity.IsPauseEntity || _dashCooldownTimer > 0f || _direction == Vector3.zero)
            return;

        _characterFxSystem.ActivateDash();
        Vector3 dashImpulse = _direction * _dashForce;
        _rigidbody.AddForce(dashImpulse, ForceMode.Impulse);

        _dashCooldownTimer = _dashCooldown;
    }
}
