using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;

namespace Core
{
    public interface ICameraService
    {
        CinemachineVirtualCameraBase MainCamera { get; }
        UniTask Initialize(CancellationToken cts);
        void SetCameraPosition(Vector3 val);
        void ResetPosition();
    }
}