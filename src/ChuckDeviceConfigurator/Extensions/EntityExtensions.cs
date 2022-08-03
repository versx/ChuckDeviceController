namespace ChuckDeviceConfigurator.Extensions
{
    using ChuckDeviceConfigurator.Utilities;
    using ChuckDeviceController.Extensions;

    public static class EntityExtensions
    {
        public static string GetLastUpdatedStatus(this ulong updated)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var isMoreThanOneDay = now - updated > Strings.OneDayS;
            var lastUpdated = updated.FromSeconds()
                                     .ToLocalTime()
                                     .ToString(Strings.DefaultDateTimeFormat);
            var updatedTime = isMoreThanOneDay
                ? updated == 0
                    ? "Never"
                    : lastUpdated
                : TimeSpanUtils.ToReadableString(updated);
            return updatedTime;
        }
    }
}