using UnityEngine;

namespace Features.Bosses.Scripts
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class WoodGuardSingleAttackPiece : MonoBehaviour
    {
        [field: SerializeField]
        [field: Tooltip("Defines the spherical warning, damage area and solid obstacle.")]
        public SphereCollider HitCollider { get; private set; }

        [field: SerializeField]
        [field: Tooltip("Center of the indicator when the roots are above ground. Size still comes from Hit Collider.")]
        public Transform IndicatorSpawnPoint { get; private set; }

        public bool HasHitCollider => HitCollider != null &&
            (HitCollider.transform == transform || HitCollider.transform.IsChildOf(transform));

        public Geometry GetGeometry(Quaternion rootRotation)
        {
            Matrix4x4 colliderToRoot = transform.worldToLocalMatrix * HitCollider.transform.localToWorldMatrix;
            Matrix4x4 rootToAttack = Matrix4x4.TRS(Vector3.zero, rootRotation, transform.localScale);
            Matrix4x4 colliderToAttack = rootToAttack * colliderToRoot;
            float scale = Mathf.Max(colliderToAttack.MultiplyVector(Vector3.right).magnitude,
                Mathf.Max(colliderToAttack.MultiplyVector(Vector3.up).magnitude,
                    colliderToAttack.MultiplyVector(Vector3.forward).magnitude));
            Vector3 centerOffset = colliderToAttack.MultiplyPoint3x4(HitCollider.center);
            return new Geometry
            {
                RootRotation = rootRotation,
                CenterOffset = centerOffset,
                IndicatorOffset = IndicatorSpawnPoint != null
                    ? rootToAttack.MultiplyPoint3x4(transform.InverseTransformPoint(IndicatorSpawnPoint.position))
                    : centerOffset,
                Radius = HitCollider.radius * scale
            };
        }

        public void EnableObstacle()
        {
            foreach (Collider pieceCollider in GetComponentsInChildren<Collider>(true))
                pieceCollider.enabled = false;
            HitCollider.isTrigger = false;
            HitCollider.enabled = true;
        }

        private void Reset() => HitCollider = GetComponent<SphereCollider>();

        public struct Geometry
        {
            public Quaternion RootRotation;
            public Vector3 CenterOffset;
            public Vector3 IndicatorOffset;
            public float Radius;
        }
    }
}
