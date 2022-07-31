namespace ChuckDeviceConfigurator.Extensions
{
    using ChuckDeviceConfigurator.Utilities;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;

    public static class DeviceExtensions
    {
        public static Device SetDeviceStatus(this Device device)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var isMoreThanOneDay = now - device.LastSeen > Strings.OneDayS;
            var lastSeen = device.LastSeen?.FromSeconds()
                                           .ToLocalTime()
                                           .ToString(Strings.DefaultDateTimeFormat);
            device.LastSeenTime = isMoreThanOneDay
                ? lastSeen
                : TimeSpanUtils.ToReadableString(device.LastSeen ?? 0);
            device.OnlineStatus = now - device.LastSeen <= Strings.DeviceOnlineThresholdS
                ? Strings.DeviceOnlineIcon
                : Strings.DeviceOfflineIcon;
            return device;
        }
    }
}