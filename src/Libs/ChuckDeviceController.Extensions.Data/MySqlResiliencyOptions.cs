namespace ChuckDeviceController.Extensions.Data
{
    public class MySqlResiliencyOptions
    {
        public const int DefaultMaxRetryCount = 10;
        public const int DefaultRetryIntervalS = 10;
        public const int DefaultCommandTimeoutS = 30;
        public const int DefaultConnectionTimeoutS = 30;
        public const int DefaultConnectionLeakTimeoutS = 120;

        public int MaxRetryCount { get; set; } = DefaultMaxRetryCount;

        public int RetryIntervalS { get; set; } = DefaultRetryIntervalS;

        public int CommandTimeoutS { get; set; } = DefaultCommandTimeoutS;

        public int ConnectionTimeoutS { get; set; } = DefaultConnectionTimeoutS;

        public int ConnectionLeakTimeoutS { get; set; } = DefaultConnectionLeakTimeoutS;
    }
}