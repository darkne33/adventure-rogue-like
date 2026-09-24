using System.Linq;

namespace Features.Relics.Scripts
{
    internal static class RelicBuildEffects
    {
        public const string SoyMilk = "soy_milk";
        public const string Polyphemus = "polyphemus";
        public const string CupidsArrow = "cupids_arrow";
        public const string RubberCement = "rubber_cement";
        public const string Explosivo = "explosivo";
        public const string Aquarius = "aquarius";
        public const string SacrificialDagger = "sacrificial_dagger";
        public const string LumpOfCoal = "lump_of_coal";
        public const string MoneyEqualsPower = "money_equals_power";

        public static string GetId(RelicEffectDefinition effect)
        {
            if (!string.IsNullOrWhiteSpace(effect.EffectPrefabId))
                return effect.EffectPrefabId.Trim();
            return string.IsNullOrWhiteSpace(effect.StatusEffectId)
                ? string.Empty : effect.StatusEffectId.Trim();
        }

        public static RelicEffectDefinition FindActive(RelicRuntimeState state, string id) =>
            state.IsBroken ? null : state.Definition.Effects?.FirstOrDefault(effect => GetId(effect) == id);

        public static bool IsPassive(RelicEffectDefinition effect) =>
            GetId(effect) is SoyMilk or Polyphemus or CupidsArrow or RubberCement or
                SacrificialDagger or LumpOfCoal or MoneyEqualsPower;
    }
}
