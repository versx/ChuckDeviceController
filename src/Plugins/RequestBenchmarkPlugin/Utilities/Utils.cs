namespace RequestBenchmarkPlugin.Utilities
{
    public static class Utils
    {
        public static string FormatTime(decimal timeMs, byte decimalPlaces = 4, bool isHtml = false)
        {
            var value = Math.Round(timeMs / 1000, decimalPlaces);
            var color = value <= 1
                ? "green"
                : value > 1 && value < 3
                    ? "orange"
                    : "red";
            return isHtml
                ? $"<span style='color: {color}'>{value}s</span>"
                : value + "s";
        }

        public static string FormatTime(decimal timeMs)
        {
            var timeS = Convert.ToUInt64(timeMs / 1000);
            var timeSpan = TimeSpan.FromMilliseconds(timeS);
            var days = timeSpan.Days;
            var hours = timeSpan.Hours;
            var minutes = timeSpan.Minutes;
            var seconds = timeSpan.Seconds;
            var ms = timeSpan.Milliseconds;
            var formatted = string.Format
            (
                "{0}{1}{2}{3}{4}",
                days > 0 ? string.Format("{0:0}d ", days) : "",
                hours > 0 ? string.Format("{0:0}h ", hours) : "",
                minutes > 0 ? string.Format("{0:0}m ", minutes) : "",
                minutes == 0 ? seconds > 0 ? string.Format("{0:0}s", seconds) : "" : "",
                seconds == 0 ? ms > 0 ? string.Format("{0:0}ms", ms) : "" : ""
            );

            var result = $"{formatted.TrimEnd(' ')}";
            return string.IsNullOrEmpty(result)
                ? "0"
                : result;
        }
    }
}