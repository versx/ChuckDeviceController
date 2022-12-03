namespace FindyJumpyPlugin.Extensions
{
    using ChuckDeviceController.Extensions;

    public static class TimeExtensions
    {
        private const ushort TimeAfterSpawn = 20;
        private const ushort MinTimer = 1500;

        public static ulong GetSecondsFromTopOfHour(this ulong seconds)
        {
            var value = seconds % 3600;
            value += (seconds % 3600) % 60;
            return value;
        }

        public static (ulong, ulong, ulong) ConvertSecondsToHoursMinutesSeconds()
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            return (now / 3600, (now % 3600) / 60, (now % 3600) % 60);
        }

        //public static (ushort, ushort) GetOffsetsForSpawnTimer(this ushort time)
        public static (uint, uint) GetOffsetsForSpawnTimer(this ushort time)
        {
            var minTime = Convert.ToUInt32(time + TimeAfterSpawn);
            var maxTime = Convert.ToUInt32(time + 1800 - MinTimer);
            return (minTime, maxTime);
        }
    }
}