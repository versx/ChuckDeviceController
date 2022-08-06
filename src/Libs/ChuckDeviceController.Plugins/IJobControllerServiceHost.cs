namespace ChuckDeviceController.Plugins
{
    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Common.Jobs;

    public interface IJobControllerServiceHost
    {
        //IReadOnlyDictionary<string, IDevice> Devices { get; }

        //IReadOnlyDictionary<string, IJobController> Instances { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="controller"></param>
        Task AddJobControllerAsync(string name, IJobController controller);
    }
}