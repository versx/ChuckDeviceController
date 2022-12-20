namespace ChuckDeviceController.Http.Proxy.Configuration;

public class ProxyConfig
{
    public string Urls { get; set; } = null!;

    public string ControllerEndpoint { get; set; } = null!;

    public string RawEndpoint { get; set; } = null!;
}