namespace ChuckDeviceController.Configuration;

public class GrpcEndpointsConfig
{
    private const string DefaultConfiguratorEndpoint = "http://localhost:5002";
    private const string DefaultCommunicatorEndpoint = "http://localhost:5003";

    public string Configurator { get; set; } = DefaultConfiguratorEndpoint;

    public string Communicator { get; set; } = DefaultCommunicatorEndpoint;
}