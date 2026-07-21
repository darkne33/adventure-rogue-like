using UnityEngine;

namespace Features.Enemies.Scripts
{
    public sealed class EnemyRangedAttackView : MonoBehaviour
    {
        [SerializeField] private Transform _projectileSpawnPoint;
        [SerializeField] private EnemyCannonball _projectilePrefab;
        [SerializeField, Min(0f)] private float _minimumAttackDistance = 6.5f;
        [SerializeField, Min(0f)] private float _windupDuration = 0.55f;
        [SerializeField, Min(0.05f)] private float _projectileFlightDuration = 1.1f;
        [SerializeField, Min(0f)] private float _projectileArcHeight = 4.2f;
        [SerializeField, Min(0f)] private float _impactRadius = 1.25f;
        [SerializeField, Min(0f)] private float _recoveryDuration = 0.35f;
        [SerializeField] private Vector3 _targetOffset = new(0f, 0.55f, 0f);

        public Transform ProjectileSpawnPoint => _projectileSpawnPoint != null ? _projectileSpawnPoint : transform;
        public EnemyCannonball ProjectilePrefab => _projectilePrefab;
        public float MinimumAttackDistance => _minimumAttackDistance;
        public float WindupDuration => _windupDuration;
        public float ProjectileFlightDuration => _projectileFlightDuration;
        public float ProjectileArcHeight => _projectileArcHeight;
        public float ImpactRadius => _impactRadius;
        public float RecoveryDuration => _recoveryDuration;
        public Vector3 TargetOffset => _targetOffset;

#if UNITY_EDITOR
        public void SetEditorReferences(Transform projectileSpawnPoint, EnemyCannonball projectilePrefab)
        {
            _projectileSpawnPoint = projectileSpawnPoint;
            _projectilePrefab = projectilePrefab;
            _minimumAttackDistance = 6.5f;
            _windupDuration = 0.55f;
            _projectileFlightDuration = 1.1f;
            _projectileArcHeight = 4.2f;
            _impactRadius = 1.25f;
            _recoveryDuration = 0.35f;
            _targetOffset = new Vector3(0f, 0.55f, 0f);
        }
#endif
    }
}
