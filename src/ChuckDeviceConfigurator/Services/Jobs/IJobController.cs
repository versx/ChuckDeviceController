namespace ChuckDeviceConfigurator.Services.Jobs
{
    using ChuckDeviceConfigurator.Services.Tasks;

    public interface IJobController : IEventController
    {
        string Name { get; }

        ushort MinimumLevel { get; }

        ushort MaximumLevel { get; }

        Task<ITask> GetTaskAsync(TaskOptions options);

        Task<string> GetStatusAsync();

        Task Stop();

        Task Reload();
    }
}
