using UnityEngine;

namespace Features.Bosses.Scripts
{
    [DisallowMultipleComponent]
    public sealed class BossSpawnPoint : MonoBehaviour
    {
        [field: SerializeField] public BossFacade BossPrefab { get; private set; }
        [SerializeField] private bool _showAttackPreview = true;

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
                return;
            Color previousColor = Gizmos.color;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.75f);
            Quaternion rotation = WoodGuardBossFacade.GetFlatRotation(transform.forward);
            Gizmos.DrawRay(transform.position, rotation * Vector3.forward * 3f);
            Gizmos.color = previousColor;

            if (!_showAttackPreview || BossPrefab is not WoodGuardBossFacade boss)
                return;
            Vector3 localOrigin = BossPrefab.transform.InverseTransformPoint(boss.AttackOrigin.position);
            Quaternion localRotation = Quaternion.Inverse(BossPrefab.transform.rotation) *
                                       boss.AttackOrigin.rotation;
            boss.DrawAttackPreview(transform.position + rotation * Vector3.Scale(localOrigin,
                BossPrefab.transform.localScale), WoodGuardBossFacade.GetFlatRotation(
                rotation * localRotation * Vector3.forward));
        }
    }
}
