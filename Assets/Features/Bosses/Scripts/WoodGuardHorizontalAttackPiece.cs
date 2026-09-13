using UnityEngine;

namespace Features.Bosses.Scripts
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class WoodGuardHorizontalAttackPiece : MonoBehaviour
    {
        [field: SerializeField]
        [field: Tooltip("Defines the piece dimensions, warning volume, damage area and solid obstacle.")]
        public BoxCollider HitCollider { get; private set; }

        public bool HasHitCollider => HitCollider != null &&
            (HitCollider.transform == transform || HitCollider.transform.IsChildOf(transform));

        public Geometry GetGeometry(Quaternion rootRotation)
        {
            Matrix4x4 boxToRoot = transform.worldToLocalMatrix * HitCollider.transform.localToWorldMatrix;
            Matrix4x4 boxToLine = Matrix4x4.TRS(Vector3.zero, rootRotation, transform.localScale) * boxToRoot;
            Vector3 size = HitCollider.size;
            Vector3 x = boxToLine.MultiplyVector(Vector3.right * size.x);
            Vector3 y = boxToLine.MultiplyVector(Vector3.up * size.y);
            Vector3 z = boxToLine.MultiplyVector(Vector3.forward * size.z);
            return new Geometry
            {
                RootRotation = rootRotation,
                BoxRotation = rootRotation * Quaternion.Inverse(transform.rotation) * HitCollider.transform.rotation,
                CenterOffset = boxToLine.MultiplyPoint3x4(HitCollider.center),
                Size = new Vector3(x.magnitude, y.magnitude, z.magnitude),
                Footprint = new Vector3(
                    Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
                    Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y),
                    Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z))
            };
        }

        public void EnableObstacle()
        {
            // Only the assigned box defines this piece's collision volume.
            foreach (Collider pieceCollider in GetComponentsInChildren<Collider>(true))
                pieceCollider.enabled = false;
            HitCollider.isTrigger = false;
            HitCollider.enabled = true;
        }

        private void Reset() => HitCollider = GetComponent<BoxCollider>();

        public struct Geometry
        {
            public Quaternion RootRotation;
            public Quaternion BoxRotation;
            public Vector3 CenterOffset;
            public Vector3 Size;
            public Vector3 Footprint;
        }
    }
}
