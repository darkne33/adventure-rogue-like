using Core;
using UnityEngine;
using Zenject;

[DisallowMultipleComponent]
public sealed class ViewToCameraObject : MonoBehaviour
{
    private enum TargetSource
    {
        Player,
        Camera,
        Custom
    }

    [SerializeField] private Transform _transformToRotate;
    [SerializeField] private TargetSource _targetSource = TargetSource.Player;
    [SerializeField] private Transform _customTarget;
    [SerializeField] private bool _faceAwayFromTarget;
    [SerializeField] private Vector3 _rotationOffset;

    [InjectOptional] private ICharacterProvider _characterProvider;
    [InjectOptional] private ICameraService _cameraService;

    private Transform _mainCameraTransform;

    public void SetTarget(Transform target)
    {
        _customTarget = target;
        _targetSource = TargetSource.Custom;
    }

    private void Reset() =>
        _transformToRotate = transform;

    private void Awake()
    {
        if (_transformToRotate == null)
            _transformToRotate = transform;
    }

    private void LateUpdate()
    {
        Transform target = GetTarget();
        if (target == null)
            return;

        Vector3 direction = target.position - _transformToRotate.position;
        direction.y = 0f;

        if (_faceAwayFromTarget)
            direction = -direction;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        _transformToRotate.rotation = targetRotation * Quaternion.Euler(_rotationOffset);
    }

    private Transform GetTarget()
    {
        switch (_targetSource)
        {
            case TargetSource.Player:
                return _characterProvider?.CharacterFacade != null
                    ? _characterProvider.CharacterFacade.transform
                    : null;

            case TargetSource.Camera:
                return GetCameraTransform();

            case TargetSource.Custom:
                return _customTarget;

            default:
                return null;
        }
    }

    private Transform GetCameraTransform()
    {
        if (_cameraService?.MainCamera != null)
            return _cameraService.MainCamera.transform;

        if (_mainCameraTransform == null && Camera.main != null)
            _mainCameraTransform = Camera.main.transform;

        return _mainCameraTransform;
    }
}
