namespace ChuckDeviceController
{
    public static class Strings
    {
        public const string BasePath = "./bin/debug/";

        public const string AppSettings = "appsettings.json";

        public const string AppSettingsFormat = "appsettings.{0}.json";

        public const int MaximumQueueBatchSize = 25;

        public const uint MaximumQueueSizeWarning = 500;

        public const ushort MaximumQueueCapacity = 8192;

        public static readonly string AssemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
    }
}