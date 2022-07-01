namespace ChuckDeviceConfigurator
{
    public static class Strings
    {
        public const string BasePath = "./bin/debug/";

        public const string AppSettings = "appsettings.json";

        public const string AppSettingsFormat = "appsettings.{0}.json";

        public static readonly string AssemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

        public const string SuperAdminRole = "SuperAdmin";

        public const string DefaultUserName = "root";

        public const string DefaultSuccessLoginPath = "/Identity/Account/Manage";
    }
}