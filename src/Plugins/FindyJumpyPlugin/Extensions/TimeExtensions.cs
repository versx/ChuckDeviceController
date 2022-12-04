namespace FindyJumpyPlugin.Extensions
{
    using ChuckDeviceController.Extensions;

    public static class TimeExtensions
    {
        private const ushort TimeAfterSpawn = 20;
        private const ushort MinTimer = 1500;
        private const ushort OneHourS = 3600;

        public static ulong GetSecondsFromTopOfHour(this ulong seconds)
        {
            var value = seconds % OneHourS;
            value += (seconds % OneHourS) % 60;
            return value;
        }

        public static (ulong, ulong, ulong) ConvertSecondsToHoursMinutesSeconds()
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            return (now / OneHourS, (now % OneHourS) / 60, (now % OneHourS) % 60);
        }

        public static (uint, uint) GetOffsetsForSpawnTimer(this ushort time)
        {
            var minTime = Convert.ToUInt32(time + TimeAfterSpawn);
            var maxTime = Convert.ToUInt32(time + 1800 - MinTimer);
            return (minTime, maxTime);
        }
    }
}