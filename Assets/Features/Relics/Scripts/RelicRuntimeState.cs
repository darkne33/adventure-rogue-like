using System.Collections.Generic;

namespace Features.Relics.Scripts
{
    public sealed class RelicRuntimeState
    {
        public RelicDefinition Definition { get; }
        public int StackCount { get; private set; }
        public bool IsBroken { get; private set; }
        public Dictionary<string, float> CooldownTimers { get; } = new();
        public Dictionary<string, float> CustomCounters { get; } = new();

        public RelicRuntimeState(RelicDefinition definition)
        {
            Definition = definition;
            StackCount = 1;
        }

        public void AddStack() =>
            StackCount++;

        public void Break() =>
            IsBroken = true;

    }
}
