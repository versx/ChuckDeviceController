namespace ChuckDeviceController.Plugins
{
    using ChuckDeviceController.Common.Jobs;

    public interface IJobControllerServiceHost
    {
        // TODO: Also include loaded devices and instances dictionaries

        Task AddJobControllerAsync(string name, IJobController controller);
    }
}