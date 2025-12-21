using System;

namespace CustomPackages.Package.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime GetFirstDayOfWeek(this DateTime date, DayOfWeek firstDayOfWeek = DayOfWeek.Monday)
        {
            int diff = (7 + (date.DayOfWeek - firstDayOfWeek)) % 7;
            return date.AddDays(-1 * diff).Date;
        }
    }
}