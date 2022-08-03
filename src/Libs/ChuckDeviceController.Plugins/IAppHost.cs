namespace ChuckDeviceController.Plugins
{
    public interface IAppHost
    {
        void Restart();

        void Shutdown();

        void Uninstall();
    }
}