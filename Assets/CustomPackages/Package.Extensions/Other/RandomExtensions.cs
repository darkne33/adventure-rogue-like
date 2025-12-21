using System;

namespace CustomPackages.Package.Extensions.Other
{
    public static class RandomExtensions
    {
        private static readonly System.Random _random = new();

        public static float GetRandom(float min, float max) =>
            UnityEngine.Random.Range(min, max);

        public static int GetRandom(int minInclusive, int maxExclusive) =>
            UnityEngine.Random.Range(minInclusive, maxExclusive);
        
        public static long GetRandom(long min, long max) =>
            _random.RandomLong(min, max);

        public static bool GetRandom() =>
            UnityEngine.Random.Range(0, 2) == 0;

        public static int GenerateRandomInt(int length)
        {
            int max = (int)Math.Pow(10, length);
            int min = max / 10;
            return _random.Next(min, max);
        }

        public static T GetScenario<T>(ValueTuple<float, T>[] data)
        {
            var randomNumber = GetRandom(0, 1f);
            float sumChances = 0;
            foreach (var chance in data)
            {
                sumChances += chance.Item1 / 100;
                if (sumChances >= randomNumber)
                    return chance.Item2;
            }

            throw new InvalidOperationException();
        }

        private static long RandomLong(this Random rnd)
        {
            byte[] buffer = new byte[8];
            rnd.NextBytes (buffer);
            return BitConverter.ToInt64(buffer, 0);
        }

        private static long RandomLong(this Random rnd, long min, long max)
        {
            EnsureMinLEQMax(ref min, ref max);
            var numbersInRange = unchecked(max - min + 1);
            if (numbersInRange < 0)
                throw new ArgumentException("Size of range between min and max must be less than or equal to Int64.MaxValue");

            var randomOffset = RandomLong(rnd);
            if (IsModuloBiased(randomOffset, numbersInRange))
                return RandomLong(rnd, min, max); // Try again
            else
                return min + PositiveModuloOrZero(randomOffset, numbersInRange);
        }

        private static bool IsModuloBiased(long randomOffset, long numbersInRange)
        {
            long greatestCompleteRange = numbersInRange * (long.MaxValue / numbersInRange);
            return randomOffset > greatestCompleteRange;
        }

        private static long PositiveModuloOrZero(long dividend, long divisor)
        {
            Math.DivRem(dividend, divisor, out var mod);
            if(mod < 0)
                mod += divisor;
            return mod;
        }

        private static void EnsureMinLEQMax(ref long min, ref long max)
        {
            if(min <= max)
                return;
            (min, max) = (max, min);
        }

        public static int GetRandom(int val) => 
            _random.Next(val);
    }
}