namespace ChuckDeviceConfigurator
{
    public static class Strings
    {
        public const string BasePath = "./bin/debug/";

        public const string AppSettings = "appsettings.json";

        public const string AppSettingsFormat = "appsettings.{0}.json";

        public static readonly string AssemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

        public static readonly string AssemblyVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public const string SuperAdminRole = "SuperAdmin";

        public const string DefaultUserName = "root";

        public const string DefaultUserPassword = "123Pa$$word.";

        public const string DefaultSuccessLoginPath = "/Identity/Account/Manage";
    }
}