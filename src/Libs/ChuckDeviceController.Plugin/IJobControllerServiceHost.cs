namespace ChuckDeviceController.Plugin
{
    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Common.Jobs;

    /// <summary>
    /// Plugin host handler contract used to interact with and manage the
    /// job controller service.
    /// </summary>
    public interface IJobControllerServiceHost : IInstanceServiceHost
    {
        //IReadOnlyDictionary<string, IDevice> Devices { get; }

        //IReadOnlyDictionary<string, IJobController> Instances { get; }

        /// <summary>
        /// Gets a list of all registered custom job controller instance types.
        /// </summary>
        IReadOnlyList<string> CustomInstanceTypes { get; }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="customInstanceType"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        Task RegisterJobControllerAsync<T>(string customInstanceType, Func<IInstance, T> factory)
            where T : IJobController;

        /// <summary>
        /// Assigns the specified device to a specific job controller
        /// instance by name.
        /// </summary>
        /// <param name="device">Device entity.</param>
        /// <param name="instanceName">Job controller instance name.</param>
        Task AssignDeviceToJobControllerAsync(IDevice device, string instanceName);
    }
}