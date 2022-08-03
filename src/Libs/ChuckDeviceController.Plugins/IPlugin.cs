namespace ChuckDeviceController.Plugins
{
    // TODO: Add support for controllers and views

    public interface IPlugin : IMetadata
    {
        //Task InitializeAsync(IServiceCollection services);
        Task InitializeAsync();
    }

    public interface IDatabaseHost
    {
    }

    public interface IDatabaseEvents
    {
    }

    public interface IUiHost
    {
        Task AddPathAsync();

        Task AddPageAsync();

        Task InheritStyling();
    }

    public interface IUiEvents
    {
    }

    public interface IControllerHost
    {
        Task AddControllerAsync();
    }

    public interface IControllerEvents
    {
    }

    public interface IJobControllerHost
    {
        Task AddJobControllerAsync();
    }

    public interface ILoggingHost
    {
        void LogMessage(string text, params object[] args);

        void LogException(Exception ex);
    }
}