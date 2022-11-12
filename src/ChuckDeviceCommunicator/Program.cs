using ChuckDeviceCommunicator.Services;
using ChuckDeviceCommunicator.Services.Rpc;
using ChuckDeviceController.Authorization.Jwt.Rpc.Interceptors;
using ChuckDeviceController.Configuration;

var config = Config.LoadConfig(args, Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
if (config.Providers.Count() == 2)
{
    // Only environment variables and command line providers added,
    // failed to load config provider.
    Environment.FailFast($"Failed to find or load configuration file, exiting...");
}

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseConfiguration(config);

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<AuthHeadersInterceptor>();

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddSingleton<IWebhookRelayService, WebhookRelayService>();
builder.Services.AddSingleton<IGrpcClientService, GrpcClientService>();
builder.Services.Configure<WebhookRelayConfig>(builder.Configuration.GetSection("Relay"));
builder.Services.Configure<GrpcEndpointsConfig>(builder.Configuration.GetSection("Grpc"));


var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<WebhookPayloadReceiverService>();
app.Run();