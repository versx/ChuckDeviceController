namespace ChuckDeviceConfigurator.Services.Jobs
{
    using ChuckDeviceConfigurator.Services.Tasks;
    using ChuckDeviceController.Data.Entities;

    public interface IJobController : IEventController
    {
        string Name { get; }

        ushort MinimumLevel { get; }

        ushort MaximumLevel { get; }

        Task<ITask> GetTaskAsync(string uuid, string? accountUsername = null, Account? account = null, bool isStartup = false);

        Task<string> GetStatusAsync();

        void Stop();

        void Reload();
    }
}
