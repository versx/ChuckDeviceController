namespace ChuckDeviceProxy;

public static class Strings
{
    public const string RawEndpoint = "/raw";
    public static readonly IReadOnlyList<string> ControllerEndpoints = new List<string>
    {
        "/controler",
        "/controller",
    };
}