using System;
using System.Collections.Generic;

namespace Plurby.Web.Infrastructure
{
    public static class TimeFormatter
    {
        public static string FormatItalian(TimeSpan timeSpan)
        {
            var hours = (int)timeSpan.TotalHours;
            var minutes = timeSpan.Minutes;

            if (hours == 0 && minutes == 0)
                return "0 minuti";

            var parts = new List<string>();

            if (hours > 0)
            {
                var hourText = hours == 1 ? "ora" : "ore";
                parts.Add($"{hours} {hourText}");
            }

            if (minutes > 0)
            {
                parts.Add($"{minutes} minuti");
            }

            return string.Join(" e ", parts);
        }

        public static string FormatItalian(TimeSpan? timeSpan)
        {
            if (!timeSpan.HasValue)
                return "";

            return FormatItalian(timeSpan.Value);
        }
    }
}