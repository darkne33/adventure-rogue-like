using Features.Relics.Scripts;
using UnityEngine;

namespace Features.FortuneWheel
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class FortuneWheelRelicRewardView : MonoBehaviour
    {
        [SerializeField] private RelicDefinition _relic;

        public RelicDefinition Relic => _relic;
    }
}
