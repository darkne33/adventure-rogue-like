using Core;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterCameraMoveSystem
{
    private readonly Transform _cameraPivot;
    private readonly CharacterCameraSettingsConfiguration _settings;
    private readonly ICameraService _cameraService;
    private readonly InputSystem_Actions _inputActions;
    private readonly PauseEntity _pauseEntity;
    private readonly Vector3 _baseCameraPivotLocalPosition;

    private float _yaw;
    private float _pitch;

    private float _yawVelocity;
    private float _pitchVelocity;
    private float _landingShakeTimer;
    private float _landingShakeDuration;
    private float _landingShakeStrength;
    private float _landingShakeVerticalOffset;

    private readonly float _topClamp = 70f;
    private readonly float _bottomClamp = -30f;

    private const float ACCELERATION = 30f;
    private const float FRICTION = 8f;
    private const float MAX_SPEED = 300f;
    private const float INPUT_THRESHOLD = 0.01f;

    public CharacterCameraMoveSystem(
        Transform cameraPivot,
        CharacterCameraSettingsConfiguration settings,
        ICameraService cameraService, PauseEntity pauseEntity)
    {
        _cameraPivot = cameraPivot;
        _settings = settings;
        _cameraService = cameraService;
        _pauseEntity = pauseEntity;

        _inputActions = new InputSystem_Actions();
        _inputActions.Enable();

        _yaw = _cameraPivot.eulerAngles.y;
        _pitch = NormalizeAngle(_cameraPivot.eulerAngles.x);
        _baseCameraPivotLocalPosition = _cameraPivot.localPosition;
        ApplyCinemachineFollowSettings();
        ResetLandingShakeBump();
    }

    public void PlayLandingShake()
    {
        float duration = Mathf.Max(0f, _settings.LandingShakeDuration);
        float strength = Mathf.Max(0f, _settings.LandingShakeStrength);
        if (duration <= 0f || strength <= 0f)
            return;

        _landingShakeDuration = duration;
        _landingShakeTimer = duration;
        _landingShakeStrength = strength;
    }

    public void StopLandingShake()
    {
        ResetLandingShakeBump();
        if (_cameraPivot != null)
            _cameraPivot.localPosition = _baseCameraPivotLocalPosition;
    }

    public void Move()
    {
        if (_cameraPivot == null || _pauseEntity.IsPauseEntity)
            return;

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

        UpdateLandingShakeBump();
        _cameraPivot.localPosition = _baseCameraPivotLocalPosition + Vector3.up * _landingShakeVerticalOffset;
        _cameraPivot.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    private bool IsMouseInput()
    {
        var control = _inputActions.Player.Look.activeControl;
        if (control == null)
            return true;

        return control.device is Mouse;
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }

    private void UpdateLandingShakeBump()
    {
        if (_landingShakeTimer <= 0f)
            return;

        _landingShakeTimer = Mathf.Max(0f, _landingShakeTimer - Time.deltaTime);

        float duration = Mathf.Max(Time.deltaTime, _landingShakeDuration);
        float elapsed = duration - _landingShakeTimer;
        float progress = Mathf.Clamp01(elapsed / duration);

        _landingShakeVerticalOffset = EvaluateLandingBumpOffset(progress) * _landingShakeStrength;

        if (_landingShakeTimer <= 0f)
            ResetLandingShakeBump();
    }

    private float EvaluateLandingBumpOffset(float progress)
    {
        if (progress < 0.34f)
            return -SmootherStep(progress / 0.34f);

        if (progress < 0.62f)
            return Mathf.Lerp(-1f, 0.28f, SmootherStep((progress - 0.34f) / 0.28f));

        return Mathf.Lerp(0.28f, 0f, SmootherStep((progress - 0.62f) / 0.38f));
    }

    private float SmootherStep(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * value * (value * (value * 6f - 15f) + 10f);
    }

    private void ResetLandingShakeBump()
    {
        _landingShakeTimer = 0f;
        _landingShakeVerticalOffset = 0f;
    }

    private void ApplyCinemachineFollowSettings()
    {
        CinemachineThirdPersonFollow follow = ResolveThirdPersonFollow();
        if (follow == null)
            return;

        follow.Damping = _settings.FollowDamping;
        follow.ShoulderOffset = _settings.FollowShoulderOffset;
        follow.VerticalArmLength = Mathf.Max(0f, _settings.FollowVerticalArmLength);
        follow.CameraDistance = Mathf.Max(0f, _settings.FollowCameraDistance);
    }

    private CinemachineThirdPersonFollow ResolveThirdPersonFollow()
    {
        CinemachineVirtualCameraBase mainCamera = _cameraService.MainCamera;
        if (mainCamera == null)
            return null;

        if (mainCamera is CinemachineCamera cinemachineCamera)
        {
            return cinemachineCamera.GetCinemachineComponent(CinemachineCore.Stage.Body) as
                CinemachineThirdPersonFollow;
        }

        return mainCamera.GetComponent<CinemachineThirdPersonFollow>();
    }

}
