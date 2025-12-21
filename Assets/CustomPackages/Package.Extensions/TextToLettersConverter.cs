using System;

namespace CustomPackages.Package.Extensions
{
    public static class TextToLettersConverter
    {
        public static string FormatValue(decimal digit, int countDigit = 2)
        {
            if (digit == 0)
                return "0";

            string[] typeValue = { "", "K", "M", "B", "T" };
            int indexer = 0;
            while (indexer + 1 < typeValue.Length && digit >= 1000m)
            {
                digit /= 1000m;
                indexer++;
            }

            digit = Math.Round(digit, countDigit);

            string formatted = $"{digit:0.##}{typeValue[indexer]}".Replace(',', '.');
            return formatted;
        }

        public static string FormatWithSpace(long number) =>
            number.ToString("N0").Replace(",", " ");
    }
}