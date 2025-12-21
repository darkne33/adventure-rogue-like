using UnityEngine;

public class CharacterCameraMoveSystem
{
    private readonly Camera _camera;
    private readonly Transform _target;
    private readonly CharacterCameraSettingsConfiguration _characterCameraSettingsConfiguration;

    private Vector3 _velocity = Vector3.zero;

    public CharacterCameraMoveSystem(Camera camera, Transform target, CharacterCameraSettingsConfiguration characterCameraSettingsConfiguration)
    {
        _camera = camera;
        _target = target;
        _characterCameraSettingsConfiguration = characterCameraSettingsConfiguration;

        _camera.transform.eulerAngles = _characterCameraSettingsConfiguration.LocalRotation;
    }

    public void Move()
    {
        if (_target == null)
            return;

        var desiredPosition = _target.position + _characterCameraSettingsConfiguration.LocalOffset;

        var currentPosition = _camera.transform.position;

        var smoothedPosition = Vector3.SmoothDamp(currentPosition, desiredPosition, ref _velocity, _characterCameraSettingsConfiguration.SmoothTime);

        _camera.transform.position = smoothedPosition;
    }
}