namespace ChuckProxy;

public static class Strings
{
    public const string ProxyHttpClientName = "ProxyClient";

    public const string RawEndpoint = "/raw";
    public static readonly IReadOnlyList<string> ControllerEndpoints = new List<string>
    {
        "/controler",
        "/controller",
    };
}