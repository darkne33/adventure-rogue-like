using TMPro;

namespace CustomPackages.Package.Extensions
{
    public static class TextMeshProExtensions
    {
        public static void SetFade(this TextMeshProUGUI text, float alpha)
        {
            var color = text.color;
            color.a = alpha;
            text.color = color;
        }
    }
}