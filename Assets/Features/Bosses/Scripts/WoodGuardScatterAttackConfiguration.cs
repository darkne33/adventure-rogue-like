using UnityEngine;

namespace Features.Bosses.Scripts
{
    public enum WoodGuardScatterPositionMode
    {
        RandomRing,
        RandomCircle,
        SequentialSides
    }

    [CreateAssetMenu(menuName = "Configs/Bosses/Scatter Wood Attack",
        fileName = "WoodGuardScatterAttackConfiguration")]
    public sealed class WoodGuardScatterAttackConfiguration : WoodGuardSingleAttackConfiguration
    {
        [field: Header("Sequence around the character")]
        [field: SerializeField, Min(1)]
        [field: Tooltip("Number of individual root pieces in one attack.")]
        public int SpawnCount { get; private set; } = 6;

        [field: SerializeField, Min(0f)]
        [field: Tooltip("Delay before the first warning, measured in boss attack time.")]
        public float InitialDelay { get; private set; } = 1f;

        [field: SerializeField, Min(0f)]
        [field: Tooltip("Minimum time between warning starts. When animating the boss, the next warning waits for the previous windup and recovery. Earlier roots can remain active.")]
        public float SpawnInterval { get; private set; } = 0.6f;

        [field: SerializeField]
        [field: Tooltip("Capture the character's current position for each new warning. When disabled, use their position at attack start. Each warned position stays fixed.")]
        public bool FollowCharacter { get; private set; } = true;

        [field: SerializeField]
        [field: Tooltip("Show a warning marker for each piece. Warning Duration still delays emergence when markers are hidden.")]
        public bool ShowWarning { get; private set; } = true;

        [field: Header("Tree attack animation")]
        [field: SerializeField, Min(0.01f)]
        [field: Tooltip("Time in seconds inside the attack clip at which the strike occurs. This pose coincides with the end of the warning and root emergence.")]
        public float AnimationImpactTime { get; private set; } = 1f;

        [field: Header("Spawn positions on the ground")]
        [field: SerializeField]
        [field: Tooltip("Random Ring uses both radii. Random Circle fills the whole Max Radius. Sequential Sides advances by Angular Step for every piece.")]
        public WoodGuardScatterPositionMode PositionMode { get; private set; } =
            WoodGuardScatterPositionMode.RandomRing;

        [field: SerializeField, Min(0f)]
        [field: Tooltip("Minimum distance from the character for Random Ring and Sequential Sides. Clamped to Max Radius.")]
        public float MinRadius { get; private set; } = 2f;

        [field: SerializeField, Min(0f)]
        [field: Tooltip("Maximum distance of a piece's target position from the character on the ground plane.")]
        public float MaxRadius { get; private set; } = 6f;

        [field: SerializeField]
        [field: Tooltip("First angle for Sequential Sides in degrees. Zero points along world forward (+Z).")]
        public float StartingAngle { get; private set; }

        [field: SerializeField]
        [field: Tooltip("Angle added for each successive piece in Sequential Sides. Negative values reverse the direction.")]
        public float AngularStep { get; private set; } = 90f;

        [field: SerializeField, Min(0f)]
        [field: Tooltip("Preferred center-to-center gap from earlier positions in this attack. If the available space cannot fit the gap, choose the most separated sampled position within the radius.")]
        public float MinimumPositionDistance { get; private set; } = 1.5f;

        public override IBossAttack CreateAttack(BossFacade boss, CharacterFacade character) =>
            new WoodGuardScatterAttack(this, boss, character);
    }
}
