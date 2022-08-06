namespace ChuckDeviceConfigurator.Utilities
{
    using ChuckDeviceController.Extensions;

    public static class TimeSpanUtils
    {
        public static string ToReadableString(ulong timestampS, bool includeAgoText = true)
        {
            var now = DateTime.UtcNow;
            var date = timestampS.FromSeconds();
            var timeSpan = TimeSpan.FromTicks(now.Ticks - date.Ticks);
            var days = timeSpan.Days;
            var hours = timeSpan.Hours;
            var minutes = timeSpan.Minutes;
            var seconds = timeSpan.Seconds;
            var formatted = string.Format
            (
                "{0}{1}{2}{3}",
                days > 0 ? string.Format("{0:0}d ", days) : "",
                hours > 0 ? string.Format("{0:0}h ", hours) : "",
                minutes > 0 ? string.Format("{0:0}m ", minutes) : "",
                minutes == 0 ? seconds > 0 ? string.Format("{0:0}s", seconds) : "" : ""
            );

            var result = string.IsNullOrEmpty(formatted)
                ? "Now"
                : $"{formatted.TrimEnd(' ')}{(includeAgoText ? " ago" : "")}";
            return result;
        }
    }
}