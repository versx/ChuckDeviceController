using ChuckDeviceCommunicator.Services;
using ChuckDeviceCommunicator.Services.Rpc;
using ChuckDeviceController.Configuration;


var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var config = Config.LoadConfig(args, env);
if (config.Providers.Count() == 2)
{
    // Only environment variables and command line providers added,
    // failed to load config provider.
    Environment.FailFast($"Failed to find or load configuration file, exiting...");
}

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseConfiguration(config);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddSingleton<IWebhookRelayService, WebhookRelayService>();
builder.Services.AddSingleton<IGrpcClientService, GrpcClientService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<WebhookPayloadReceiverService>();

app.Run();