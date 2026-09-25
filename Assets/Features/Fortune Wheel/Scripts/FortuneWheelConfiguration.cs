using System;
using System.Collections.Generic;
using Features.Relics.Scripts;
using UnityEngine;

namespace Features.FortuneWheel
{
    public enum FortuneWheelRewardType
    {
        None,
        Heart,
        Key,
        Gold,
        Relic
    }

    [Serializable]
    public sealed class FortuneWheelReward
    {
        [SerializeField] private FortuneWheelRewardType _type;
        [SerializeField] private RelicRarity _rarity;
        [SerializeField, Min(1)] private int _amount = 1;

        public FortuneWheelRewardType Type => _type;
        public RelicRarity Rarity => _rarity;
        public int Amount => Mathf.Max(1, _amount);
    }

    [Serializable]
    public sealed class FortuneWheelRewardSet
    {
        [SerializeField, Min(1)] private int _spinCost = 1;
        [SerializeField] private FortuneWheelReward[] _rewards = Array.Empty<FortuneWheelReward>();

        public int SpinCost => Mathf.Max(1, _spinCost);
        public IReadOnlyList<FortuneWheelReward> Rewards => _rewards;
    }

    [CreateAssetMenu(menuName = "Configurations/Fortune Wheel", fileName = "FortuneWheelConfiguration")]
    public sealed class FortuneWheelConfiguration : ScriptableObject
    {
        [SerializeField] private FortuneWheelRewardSet[] _rewardSets = Array.Empty<FortuneWheelRewardSet>();

        public IReadOnlyList<FortuneWheelRewardSet> RewardSets => _rewardSets;
    }
}
