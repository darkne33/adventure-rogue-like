using System;
using JsonSubTypes;
using Newtonsoft.Json;

namespace Core
{
    [Serializable]
    [JsonConverter(typeof(JsonSubtypes), "BreedReward")]
    [JsonSubtypes.KnownSubType(typeof(MoneyReward), "MoneyReward")]
    public abstract class RewardBase
    {
        public abstract string BreedReward { get; }
    }
}