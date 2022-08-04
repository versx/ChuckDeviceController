namespace ChuckDeviceController.Plugins
{
    public interface ILoggingHost
    {
        void LogMessage(string text, params object[] args);

        void LogException(Exception ex);
    }
}