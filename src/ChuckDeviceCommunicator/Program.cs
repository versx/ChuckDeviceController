using ChuckDeviceCommunicator.Services;
using ChuckDeviceCommunicator.Services.Rpc;
using ChuckDeviceController.Authorization.Jwt.Rpc.Interceptors;
using ChuckDeviceController.Configuration;
using ChuckDeviceController.Protos;


#region Config

var config = Config.LoadConfig(args, Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
if (config.Providers.Count() == 2)
{
    // Only environment variables and command line providers added,
    // failed to load config provider.
    Environment.FailFast($"Failed to find or load configuration file, exiting...");
}
var grpcConfig = new GrpcEndpointsConfig();
config.Bind("Grpc", grpcConfig);

var webhookConfig = new WebhookRelayConfig();
config.Bind("Relay", webhookConfig);

#endregion

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseConfiguration(config);

#region Services

builder.Services.AddHttpContextAccessor();

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.Configure<WebhookRelayConfig>(config.GetSection("Relay"));
builder.Services.Configure<GrpcEndpointsConfig>(config.GetSection("Grpc"));

builder.Services.AddSingleton<IWebhookRelayService, WebhookRelayService>();
builder.Services.AddSingleton<IGrpcClient<WebhookEndpoint.WebhookEndpointClient, WebhookEndpointRequest, WebhookEndpointResponse>, GrpcWebhookEndpointsClient>();
builder.Services.AddSingleton<AuthHeadersInterceptor>();

if (!string.IsNullOrEmpty(grpcConfig.Configurator))
{
    builder.Services
        .AddGrpcClient<WebhookEndpoint.WebhookEndpointClient>(options => options.Address = new Uri(grpcConfig.Configurator))
        .AddInterceptor<AuthHeadersInterceptor>();
}

#endregion

#region App Builder

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<WebhookPayloadReceiverService>();
app.Run();

#endregion