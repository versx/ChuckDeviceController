namespace ChuckDeviceController.Plugins
{
    using ChuckDeviceController.Common;
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Common.Jobs;

    public interface IJobControllerServiceHost
    {
        Task RegisterAllJobControllerTypesAsync();

        Task RegisterJobControllerTypeAsync(InstanceType type);

        Task AddJobControllerAsync(string name, InstanceType type, IJobController controller);
    }
}