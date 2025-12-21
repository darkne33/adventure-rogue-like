using UnityEngine.UI;

namespace CustomPackages.Package.Extensions
{
    public static class ImageExtensions
    {
        public static void SetAlfa(this Image image, float val)
        {
            var color = image.color;
            color.a = val;
            image.color = color;
        }
    }
}