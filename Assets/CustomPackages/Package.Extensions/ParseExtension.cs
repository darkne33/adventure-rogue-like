using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core;

namespace CustomPackages.Package.Extensions
{
    public static class ParseExtension
    {
        public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        private const string EMPTY_FLAG = "-";

        public static string CheckForEmptiness(this string field)
        {
            if (IsFieldEmpty(field)) throw new Exception("NotEmpty field dont have value");

            return field;
        }

        public static int TryParseToInt(this string field)
        {
            if (IsFieldEmpty(field)) return default;
            const string deleteAnySpase = @"\s+";
            var normalizedField = new Regex(deleteAnySpase).Replace(field, string.Empty);

            return int.Parse(normalizedField);
        }
        
        public static long TryParseToLong(this string field)
        {
            if (IsFieldEmpty(field)) return default;
            const string deleteAnySpase = @"\s+";
            var normalizedField = new Regex(deleteAnySpase).Replace(field, string.Empty);

            return long.Parse(normalizedField);
        }

        public static string[] TryParseToStringArray(this string field)
        {
            if (IsFieldEmpty(field)) return default;
            const string deleteAnySpase = @"\s+";
            return new Regex(deleteAnySpase).Replace(field, string.Empty).Split(',');
        }

        public static int[] TryParseToIntArray(this string field)
        {
            if (IsFieldEmpty(field)) return default;
            const string deleteAnySpase = @"\s+";
            var normalizedField = new Regex(deleteAnySpase).Replace(field, string.Empty);
            var elements = normalizedField.Split(',');
            int[] arr = new int[elements.Length];
            for (var index = 0; index < elements.Length; index++)
            {
                var element = elements[index];
                var cleanStr = element.Replace(",", string.Empty);
                arr[index] = int.Parse(cleanStr);
            }

            return arr;
        }

        public static float[] TryParseToFloatArray(this string field)
        {
            if (IsFieldEmpty(field)) return default;
            const string deleteAnySpase = @"\s+";
            var normalizedField = new Regex(deleteAnySpase).Replace(field, string.Empty);
            var elements = normalizedField.Split(',');
            float[] arr = new float[elements.Length];
            for (var index = 0; index < elements.Length; index++)
            {
                var element = elements[index];
                var cleanStr = element.Replace(",", string.Empty);
                arr[index] = float.TryParse(cleanStr, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var result)
                    ? result
                    : default;
            }

            return arr;
        }
        
        public static long[] TryParseToLongArray(this string field)
        {
            if (IsFieldEmpty(field)) return default;
            const string deleteAnySpase = @"\s+";
            var normalizedField = new Regex(deleteAnySpase).Replace(field, string.Empty);
            var elements = normalizedField.Split(',');
            long[] arr = new long[elements.Length];
            for (var index = 0; index < elements.Length; index++)
            {
                var element = elements[index];
                var cleanStr = element.Replace(",", string.Empty);
                arr[index] = long.Parse(cleanStr);
            }

            return arr;
        }

        public static T TryParseToEnum<T>(this string field) where T : struct
        {
            if (IsFieldEmpty(field)) return default;
            const string deleteAnySpase = @"\s+";
            var normalizedField = new Regex(deleteAnySpase).Replace(field, string.Empty);

            return Enum.TryParse<T>(normalizedField, out var result) ? result : default;
        }

        public static bool TryParseToBool(this string field)
        {
            if (IsFieldEmpty(field)) return false;
            const string deleteAnySpase = @"\s+";
            var normalizedField = new Regex(deleteAnySpase).Replace(field, string.Empty).ToLower();

            return bool.Parse(normalizedField);
        }

        public static float TryParseToFloat(this string field)
        {
            if (IsFieldEmpty(field)) return default;

            const char point = '.';
            const char comma = ',';

            return float.Parse(field.Replace(comma, point), CultureInfo.InvariantCulture);
        }

        public static DateTime StringToDateTime(this string dateTimeString)
        {
            return DateTime.TryParseExact(dateTimeString, DateTimeFormat, null, DateTimeStyles.AdjustToUniversal,
                out DateTime parsedDate)
                ? parsedDate
                : default;
        }

        public static TimeSpan StringToTimeSpan(this string timeString)
        {
            if (string.IsNullOrEmpty(timeString))
            {
                Log.GlobalEvent.Error("Time string cannot be null or empty");
                return TimeSpan.FromHours(24);
            }
        
            var parts = timeString.Split(':');

            if (parts.Length != 3)
            {
                Log.GlobalEvent.Error("Time string must be in format h:mm:ss");
                return TimeSpan.FromHours(24);
            }
        
            if (int.TryParse(parts[0], out int hours) &&
                int.TryParse(parts[1], out int minutes) &&
                int.TryParse(parts[2], out int seconds))
            {
                return new TimeSpan(hours, minutes, seconds);
            }
            
            Log.GlobalEvent.Error("Invalid time format");
            return TimeSpan.FromHours(24);
        }

        private static bool IsFieldEmpty(this string field) => 
            field is EMPTY_FLAG || string.IsNullOrEmpty(field);
    }
}