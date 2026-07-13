using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Scripting;
using Zenject;

namespace Core
{
    [Preserve]
    public sealed class CameraService : ICameraService
    {
        public CinemachineVirtualCameraBase MainCamera { get; private set; }

        [Inject] private IAddressableLoadService _assets;
        [Inject] private DiContainer _diContainer;
        [Inject] private CameraFactoryConfig _cameraFactoryConfig;

        private Vector3 _defaultCameraPosition;
        private float _defaultCameraSize;

        public async UniTask Initialize(CancellationToken cts)
        {
            await _cameraFactoryConfig.MainCamera.Load(cts);
            GameObject camera = _diContainer.InstantiatePrefab(_cameraFactoryConfig.MainCamera.Get(),
                _diContainer.DefaultParent);
            MainCamera = camera.GetComponent<CinemachineVirtualCameraBase>();
            _defaultCameraPosition = MainCamera.transform.position;
        }

        public void SetCameraPosition(Vector3 val)
        {
            val.x = _defaultCameraPosition.x;
            val.z = _defaultCameraPosition.z;
            MainCamera.transform.position = val;
        }

        public void Reset()
        {
            MainCamera.transform.position = _defaultCameraPosition;
        }

        public void ResetPosition()
        {
            DOTween.Kill(MainCamera.transform);
            MainCamera.transform.position = _defaultCameraPosition;
        }
    }
}
