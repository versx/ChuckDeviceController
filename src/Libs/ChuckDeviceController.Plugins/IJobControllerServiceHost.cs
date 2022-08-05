namespace ChuckDeviceController.Plugins
{
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Common.Jobs;

    public interface IJobControllerServiceHost
    {
        // TODO: Possibly include list of registered job controller types
        // TODO: Also include loaded devices and instances dictionaries

        Task RegisterAllJobControllerTypesAsync();

        Task RegisterJobControllerTypeAsync(InstanceType type);

        Task AddJobControllerAsync(string name, InstanceType type, IJobController controller);
    }
}