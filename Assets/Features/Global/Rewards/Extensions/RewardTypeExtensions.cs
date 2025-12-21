using System;
using CustomPackages.Package.Extensions;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core;

namespace Core
{
    public static class RewardTypeExtensions
    {
        public static RewardBase CreateReward(this MainRewardType rewardType, string val)
        {
            switch (rewardType)
            {
                case MainRewardType.None:
                    return null;
                case MainRewardType.Money:
                    return new MoneyReward { MoneyRewardAmount = val.TryParseToLong() };
                default:
                    throw new ArgumentOutOfRangeException(nameof(rewardType), rewardType, val);
            }
        }

        public static RewardBase DefaultExceptionReward()
        {
            Log.Gameplay.Error("Something goes wrong, create Default Reward ");
            return MainRewardType.Money.CreateReward("10000");
        }
    }
}