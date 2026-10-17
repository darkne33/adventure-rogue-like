using UnityEngine;

public static class CharacterHealthRules
{
    public const float HalfHeart = 0.5f;
    public const float MaximumHearts = 12f;
    public const float LegacyHealthPerHeart = 15f;

    public static float ClampMaximum(float hearts) =>
        Mathf.Clamp(Mathf.Round(hearts / HalfHeart) * HalfHeart, HalfHeart, MaximumHearts);

    public static float NormalizeDamage(float hearts) =>
        hearts <= 0f ? 0f : Mathf.Max(HalfHeart, Mathf.Ceil(hearts / HalfHeart - 0.0001f) * HalfHeart);

    public static float FromLegacyHealth(float health) => health / LegacyHealthPerHeart;
}
