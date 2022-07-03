namespace ChuckDeviceConfigurator.Services.Jobs
{
    public interface IEventController
    {
        string GroupName { get; }

        bool IsEvent { get; }
    }
}