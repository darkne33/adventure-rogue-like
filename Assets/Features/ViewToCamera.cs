using Core;
using UnityEngine;
using Zenject;

public class ViewToCamera : MonoBehaviour
{
    [SerializeField] private RectTransform _root;
    [SerializeField] private bool _isAllAxis;

    [Inject] private ICameraService _cameraService;

    private Vector3 _scaleMagnitude;
    private bool _isScaleInitialized;

    public void Initialize(RectTransform root, bool isAllAxis)
    {
        _root = root;
        _isAllAxis = isAllAxis;
        CacheScale();
    }

    private void LateUpdate()
    {
        if (_root == null || _cameraService?.MainCamera == null)
            return;

        if (!_isScaleInitialized)
            CacheScale();

        CompensateParentScale();

        Vector3 cameraPosition = _cameraService.MainCamera.transform.position;
        Vector3 targetPosition = new(cameraPosition.x,
            _isAllAxis ? cameraPosition.y : _root.position.y,
            cameraPosition.z);
        Vector3 directionAwayFromCamera = _root.position - targetPosition;

        if (directionAwayFromCamera.sqrMagnitude > 0.001f)
            _root.rotation = Quaternion.LookRotation(directionAwayFromCamera.normalized, Vector3.up);
    }

    private void CacheScale()
    {
        if (_root == null)
            return;

        Vector3 localScale = _root.localScale;
        _scaleMagnitude = new Vector3(
            Mathf.Abs(localScale.x),
            Mathf.Abs(localScale.y),
            Mathf.Abs(localScale.z));
        _isScaleInitialized = true;
    }

    private void CompensateParentScale()
    {
        Vector3 parentScale = _root.parent != null
            ? _root.parent.lossyScale
            : Vector3.one;

        _root.localScale = new Vector3(
            _scaleMagnitude.x * GetScaleSign(parentScale.x),
            _scaleMagnitude.y * GetScaleSign(parentScale.y),
            _scaleMagnitude.z * GetScaleSign(parentScale.z));
    }

    private static float GetScaleSign(float scale) =>
        scale < 0f ? -1f : 1f;
}
