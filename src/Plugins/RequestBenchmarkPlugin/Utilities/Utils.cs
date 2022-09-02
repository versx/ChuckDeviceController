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
    }
}