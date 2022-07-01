namespace ChuckDeviceConfigurator.Services.Jobs
{
    using ChuckDeviceController.Data.Entities;

    public interface IJobControllerService
    {
        void Start();

        void Stop();

        #region Instances

        IJobController GetInstanceController(string uuid);

        Task<string> GetStatusAsync(Instance instance);

        Task AddInstanceAsync(Instance instance);

        Task ReloadInstanceAsync(Instance newInstance, string oldInstanceName);

        void ReloadAll();

        Task RemoveInstanceAsync(string instanceName);

        #endregion

        #region Devices

        void AddDevice(Device device);

        Task RemoveDeviceAsync(Device device);

        void RemoveDevice(string uuid);

        void ReloadDevice(Device device, string oldDeviceUuid);

        List<string> GetDeviceUuidsInInstance(string instanceName);

        #endregion
    }
}