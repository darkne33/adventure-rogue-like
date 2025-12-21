using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    public interface ICameraService
    {
        Camera MainCamera { get; }
        UniTask Initialize(CancellationToken cts);
        void SetCameraPosition(Vector3 val);
        void Reset();
        void ResetPosition();
        void SetOrthographicCamera(float val);
    }
}