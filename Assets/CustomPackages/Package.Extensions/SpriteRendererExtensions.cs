using UnityEngine;

namespace CustomPackages.Package.Extensions
{
    public static class SpriteRendererExtensions
    {
        public static void SetFade(this SpriteRenderer spriteRenderer, float fade)
        {
            var color = spriteRenderer.color;
            color.a = fade;
            spriteRenderer.color = color;
        }
    }
}