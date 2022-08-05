namespace ChuckDeviceConfigurator.Services.Plugins.Hosts
{
    using ChuckDeviceController.Plugins;

    public class UiHost : IUiHost
    {
        public Task AddNavbarHeaderAsync(NavbarHeaderOptions options)
        {
            return Task.CompletedTask;
        }

        public Task AddPathAsync()
        {
            return Task.CompletedTask;
        }
    }
}