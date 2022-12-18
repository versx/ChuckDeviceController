namespace ChuckDeviceController.Extensions.Data
{
    public class MySqlResiliencyOptions
    {
        public const ushort DefaultMaximumPoolSize = 1024;
        public const int DefaultMaximumRetryCount = 10;
        public const int DefaultRetryIntervalS = 10;
        public const int DefaultCommandTimeoutS = 30;
        public const int DefaultConnectionTimeoutS = 30;
        public const int DefaultConnectionLeakTimeoutS = 300;

        public ushort MaximumPoolSize { get; set; } = DefaultMaximumPoolSize;

        public int MaximumRetryCount { get; set; } = DefaultMaximumRetryCount;

        public int RetryIntervalS { get; set; } = DefaultRetryIntervalS;

        public int CommandTimeoutS { get; set; } = DefaultCommandTimeoutS;

        public int ConnectionTimeoutS { get; set; } = DefaultConnectionTimeoutS;

        public int ConnectionLeakTimeoutS { get; set; } = DefaultConnectionLeakTimeoutS;
    }
}