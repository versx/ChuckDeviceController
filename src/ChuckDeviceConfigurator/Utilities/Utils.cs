namespace ChuckDeviceConfigurator.Utilities
{
    public static class Utils
    {
        public static string FormatAssignmentTime(uint timeS)
        {
            var times = TimeSpan.FromSeconds(timeS);
            return timeS == 0
                ? "On Complete" // TODO: Localize
                : $"{times.Hours:00}:{times.Minutes:00}:{times.Seconds:00}";
        }
    }
}