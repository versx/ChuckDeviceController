namespace ChuckDeviceConfigurator.Services.Jobs
{
    public interface IJobController
    {
        string Name { get; }

        ushort MinimumLevel { get; }

        ushort MaximumLevel { get; }

        string GroupName { get; }

        bool IsEvent { get; }


        Task<string> GetStatus();

        void Reload();
    }
}
