namespace ChuckDeviceController.Extensions
{
    using System;

    public static class DateTimeExtensions
    {
        public static ulong ToTotalSeconds(this DateTime dateTime)
        {
            // According to Wikipedia, there are 10,000,000 ticks in a second, and Now.Ticks is the span since 1/1/0001. 
            //var seconds = dateTime.Ticks / 10000000;
            //return Convert.ToUInt64(seconds);
            double unixTimestamp = dateTime.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            return Convert.ToUInt64(unixTimestamp);
        }

        public static ulong SecondsUntilMidnight(this DateTime dateTime)
        {
            double seconds = DateTime.Today.AddDays(1).Subtract(dateTime).TotalSeconds;
            return Convert.ToUInt64(seconds);
        }

        public static DateTime FromUnix(this ulong unixSeconds)
        {
            DateTime epochTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            DateTime localDateTime = epochTime.AddSeconds(unixSeconds);//.ToLocalTime();

            return localDateTime;
        }

        public static DateTime FromMilliseconds(this ulong unixMs)
        {
            return new DateTime(1970, 1, 1).AddMilliseconds(unixMs);
        }
    }
}