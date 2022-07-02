namespace ChuckDeviceConfigurator.Services.Jobs
{
    using ChuckDeviceConfigurator.Services.Tasks;

    public interface IJobController
    {
        string Name { get; }

        ushort MinimumLevel { get; }

        ushort MaximumLevel { get; }

        string GroupName { get; }

        bool IsEvent { get; }


        Task<ITask> GetTaskAsync(string uuid, string accountUsername, bool isStartup);

        Task<string> GetStatusAsync();

        void Stop();

        void Reload();
    }
}
