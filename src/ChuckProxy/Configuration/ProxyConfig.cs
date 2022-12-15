namespace ChuckProxy.Configuration;

public class ProxyConfig
{
    public string Urls { get; set; } = null!;

    public string ControllerEndpoint { get; set; } = null!;

    public List<string> RawEndpoints { get; set; } = new List<string>();
}