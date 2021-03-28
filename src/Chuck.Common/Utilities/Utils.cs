namespace Chuck.Common.Utilities
{
    using System;

    public static class Utils
    {
        public static string FormatTime(uint time)
        {
            var times = TimeSpan.FromSeconds(time);
            return time == 0
                ? "On Complete"
                : $"{times.Hours:00}:{times.Minutes:00}:{times.Seconds:00}";
        }
    }
}