namespace ChuckDeviceController.Common.Tasks
{
    using ChuckDeviceController.Common.Data;

    /// <summary>
    /// Job controller instance task options used when requesting a
    /// new job.
    /// </summary>
    public class TaskOptions
    {
        /// <summary>
        /// Gets or sets the device UUID that is requesting the job.
        /// </summary>
        public string Uuid { get; set; }

        /// <summary>
        /// Gets or sets the account username the device is assigned to.
        /// </summary>
        public string? AccountUsername { get; set; } = null;

        /// <summary>
        /// Gets or sets the account object of the account username
        /// assigned to the device.
        /// </summary>
        public IAccount? Account { get; set; } = null;
    }
}