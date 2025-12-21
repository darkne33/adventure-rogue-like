using System;

namespace CustomPackages.Package.Extensions
{
    public static class MathExtensions
    {
        public static float GetClampedValueFromZeroToOne(float originalValue, float minOriginalRange,
            float maxOriginalRange,
            float minNewRange, float maxNewRange)
        {
            var newValue = minNewRange
                           + (maxNewRange - minNewRange) 
                           * (originalValue - minOriginalRange)
                           / (maxOriginalRange - minOriginalRange);
            return newValue;
        }

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        public static float InverseLerpUnclamped(float a, float b, float value) =>
            a != b ? (value - a) / (b - a) : 0.0f;
        
        public static double Normalize(double val, double valmin, double valmax, double min, double max) 
        {
            return (((val - valmin) / (valmax - valmin)) * (max - min)) + min;
        }
        
        public static long RoundToNearestThousand(double number) =>
            (long)(Math.Round(number / 1000.0) * 1000);
        
        public static long RoundLongToTwoSignificantDigits(long number)
        {
            if (number == 0)
                return 0;

            long absNumber = Math.Abs(number);
            int digits = (int)Math.Log10(absNumber) + 1; // Кол-во цифр в числе
            int power = digits - 2; // Степень для оставления 2 значащих цифр

            if (power <= 0)
                return number; // Если число меньше 100, округлять не нужно

            long divisor = (long)Math.Pow(10, power);
            long roundedTwoDigits = (long)Math.Round((double)absNumber / divisor, MidpointRounding.AwayFromZero);
            long result = roundedTwoDigits * divisor;

            return number < 0 ? -result : result; // Учитываем знак
        }
    }
}