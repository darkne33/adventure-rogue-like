using System.Collections.Generic;

namespace CustomPackages.Package.Extensions
{
    public static class DictionaryExtensions
    {
        public static void DistributeDecrease<T>(this Dictionary<T, int> source, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var randomPair = source.GetRandom();

                source[randomPair.Key]--;

                if (source[randomPair.Key] == 0)
                {
                    source.Remove(randomPair.Key);
                }
            }
        }
    }
}
