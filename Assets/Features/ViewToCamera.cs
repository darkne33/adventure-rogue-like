using Core;
using UnityEngine;
using Zenject;

public class ViewToCamera : MonoBehaviour
{
    [SerializeField] private RectTransform _root;

    [Inject] private ICameraService _cameraService;

    [SerializeField] private bool _isAllAxis;

    private void LateUpdate()
    {
        _root.LookAt(new Vector3(_cameraService.MainCamera.transform.position.x,
            _isAllAxis ? _cameraService.MainCamera.transform.position.y : _root.position.y,
            _cameraService.MainCamera.transform.position.z));
    }
}