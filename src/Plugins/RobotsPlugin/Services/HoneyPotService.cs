namespace RobotsPlugin.Services;

using ChuckDeviceController.Plugin;
using ChuckDeviceController.Plugin.Services;

/// <summary>
/// Honeypot logger service for web crawler bots
/// </summary>
[
    PluginService(
        ServiceType = typeof(IHoneyPotService),
        ProxyType = typeof(HoneyPotService),
        Provider = PluginServiceProvider.Plugin,
        Lifetime = ServiceLifetime.Singleton
    )
]
public class HoneyPotService : IHoneyPotService
{
    private const string DefaultLogFileName = "honeypot.txt";
    private const string DefaultLogFolderName = "logs";

    private readonly IFileStorageHost _fileStorageHost;
    private readonly ILoggingHost _loggingHost;

    public HoneyPotService(
        IFileStorageHost fileStorageHost,
        ILoggingHost loggingHost)
    {
        _fileStorageHost = fileStorageHost;
        _loggingHost = loggingHost;
    }

    public void OnTriggered(string ipAddress, string userAgent)
    {
        // Log to honeypot.txt file in root of plugin's folder
        _loggingHost.LogInformation($"Honey pot triggered by web crawler bot: {ipAddress} - {userAgent}");

        // Check if throws error when not found
        var log = _fileStorageHost.Load<List<HoneyPotEvent>>(DefaultLogFolderName, DefaultLogFileName) ?? new();
        log.Add(new(ipAddress, userAgent));

        _fileStorageHost.Save(log, DefaultLogFolderName, DefaultLogFileName, prettyPrint: true);
    }

    private class HoneyPotEvent
    {
        public string IpAddress { get; set; }

        public string UserAgent { get; set; }

        public HoneyPotEvent(string ipAddress, string userAgent)
        {
            IpAddress = ipAddress;
            UserAgent = userAgent;
        }
    }
}