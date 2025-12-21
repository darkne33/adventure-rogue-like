using UnityEngine;

namespace CustomPackages.Package.Extensions.Camera
{
    public static class CameraExtensions
    {
        public static Vector2 GetScreenPositionFromViewport(this UnityEngine.Camera _mainCamera, Vector2 canvasSize, Transform target, Vector3 offset = default)
        {
            var viewPort = _mainCamera.WorldToViewportPoint(target.transform.position + offset);
            var screenPosition = new Vector2(canvasSize.x * viewPort.x,
                canvasSize.y * viewPort.y);
            return screenPosition;
        }
    }
}