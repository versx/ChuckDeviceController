namespace ChuckDeviceController.Plugins
{
    public interface IAppEvents
    {
        Task OnInitializedAsync();

        Task OnStopAsync();
    }
}