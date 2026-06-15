using UnityEngine;

namespace Features.Relics.Scripts
{
    [CreateAssetMenu(menuName = "Configs/Relics/Relic Pool")]
    public sealed class RelicPoolConfiguration : ScriptableObject
    {
        [field: SerializeField] public RelicDefinition[] Relics { get; private set; }
        [field: SerializeField] public int CommonWeight { get; private set; } = 68;
        [field: SerializeField] public int UncommonWeight { get; private set; } = 22;
        [field: SerializeField] public int RareWeight { get; private set; } = 9;
        [field: SerializeField] public int LegendaryWeight { get; private set; } = 1;

        public int GetWeight(RelicRarity rarity) =>
            rarity switch
            {
                RelicRarity.Common => CommonWeight,
                RelicRarity.Uncommon => UncommonWeight,
                RelicRarity.Rare => RareWeight,
                RelicRarity.Legendary => LegendaryWeight,
                _ => 0
            };
    }
}
