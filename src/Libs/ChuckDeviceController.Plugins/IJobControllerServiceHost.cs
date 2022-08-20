namespace ChuckDeviceController.Plugin
{
    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Common.Jobs;

    /// <summary>
    /// Plugin host handler contract used to interact with and manage the
    /// job controller service.
    /// </summary>
    public interface IJobControllerServiceHost
    {
        //IReadOnlyDictionary<string, IDevice> Devices { get; }

        //IReadOnlyDictionary<string, IJobController> Instances { get; }

        Task CreateInstanceTypeAsync(IInstanceCreationOptions options);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="controller"></param>
        Task AddJobControllerAsync(string name, IJobController controller);

        /// <summary>
        /// Assigns the specified device to a specific job controller
        /// instance by name.
        /// </summary>
        /// <param name="device">Device entity.</param>
        /// <param name="jobControllerName">Job controller instance name.</param>
        Task AssignDeviceToJobControllerAsync(IDevice device, string jobControllerName);
    }
}