namespace ChuckDeviceController.Configuration
{
    using Microsoft.Extensions.Configuration;

    public static class Config
    {
        private const string BasePath = "./bin/debug/";
        private const string AppSettings = "appsettings.json";
        private const string AppSettingsFormat = "appsettings.{0}.json";

        public static IConfigurationRoot LoadConfig(string[] args, string env = "")
        {
            var baseFilePath = Path.Combine(BasePath, AppSettings);
            var envFilePath = Path.Combine(BasePath, string.Format(AppSettingsFormat, env));

            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory());
            if (File.Exists(baseFilePath))
            {
                configBuilder = configBuilder.AddJsonFile(baseFilePath, optional: false, reloadOnChange: true);
            }
            if (File.Exists(envFilePath))
            {
                configBuilder = configBuilder.AddJsonFile(envFilePath, optional: true, reloadOnChange: true);
            }
            var config = configBuilder.AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();
            return config;
        }
    }
}