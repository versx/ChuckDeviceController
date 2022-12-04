namespace ChuckDeviceController.Configuration
{
    public class WebhookRelayConfig
    {
        public const ushort DefaultMaximumRetryCount = 3;
        public const ushort DefaultEndpointsIntervalS = 60; // 60 seconds
        public const ushort DefaultFailedRetryDelayS = 3; // 3 seconds
        public const ushort DefaultProcessingIntervalS = 5; // 5 seconds
        public const ushort DefaultRequestTimeoutS = 15; // 15 seconds

        /// <summary>
        /// Gets or sets the interval in seconds to request webhook endpoints. (Default: 60 seconds)
        /// </summary>
        public ushort EndpointsIntervalS { get; set; } = DefaultEndpointsIntervalS;

        /// <summary>
        /// Gets or sets the amount of time to wait in seconds before attempting
        /// to send a failed webhook again. (Default: 3 seconds)
        /// </summary>
        public ushort FailedRetryDelayS { get; set; } = DefaultFailedRetryDelayS;

        /// <summary>
        /// Gets or sets the maximum amount of retry attempts for failed webhooks. (Default: 3)
        /// </summary>
        public ushort MaximumRetryCount { get; set; } = DefaultMaximumRetryCount;

        /// <summary>
        /// Gets or sets the amount of time in seconds to wait between checking
        /// if webhooks need to be sent out. (Default: 5 seconds)
        /// </summary>
        public ushort ProcessingIntervalS { get; set; } = DefaultProcessingIntervalS;

        /// <summary>
        /// Gets or sets the webhook request timeout in seconds. (Default: 15 seconds)
        /// </summary>
        public ushort RequestTimeoutS { get; set; } = DefaultRequestTimeoutS;
    }
}