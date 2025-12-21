using System;

namespace Core
{
    [Serializable]
    public class MoneyReward : RewardBase
    {
        public override string BreedReward => "MoneyReward";
        public long MoneyRewardAmount;
    }
}