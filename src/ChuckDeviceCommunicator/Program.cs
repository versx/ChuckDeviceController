using ChuckDeviceCommunicator.Services;
using ChuckDeviceCommunicator.Services.Rpc;
using ChuckDeviceController.Authorization.Jwt.Extensions;
using ChuckDeviceController.Configuration;
using ChuckDeviceController.Logging;
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

#region Logging Filtering

var logLevel = config.GetSection("Logging:LogLevel:Default").Get<LogLevel>();
builder.WebHost.ConfigureLogging(configure =>
{
    configure.ClearProviders();
    var loggingSection = config.GetSection("Logging");
    var loggingConfig = new ColorConsoleLoggerConfiguration();
    var colorLoggingSection = loggingSection.GetSection("ColorConsole");
    colorLoggingSection.Bind(loggingConfig);
    configure.AddColorConsoleLogger(options =>
    {
        options.LogLevelColorMap = loggingConfig.LogLevelColorMap;
    });
    configure.AddFile(loggingSection, options =>
    {
        var time = loggingConfig.UseUnix ? DateTime.UtcNow : DateTime.Now;
        options.FormatLogFileName = fileName => string.Format(fileName, time);
        options.UseUtcTimestamp = true;
    });
    configure.GetLoggingConfig(logLevel);
});

#endregion

#region Services

builder.Services.AddHttpContextAccessor();

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.Configure<WebhookRelayConfig>(config.GetSection("Relay"));
builder.Services.Configure<GrpcEndpointsConfig>(config.GetSection("Grpc"));

builder.Services.AddSingleton<IWebhookRelayService, WebhookRelayService>();
builder.Services.AddSingleton<IGrpcClient<WebhookEndpoint.WebhookEndpointClient, WebhookEndpointRequest, WebhookEndpointResponse>, GrpcWebhookEndpointsClient>();

if (!string.IsNullOrEmpty(grpcConfig.Configurator))
{
    builder.Services
        .AddGrpcClient<WebhookEndpoint.WebhookEndpointClient>(options => options.Address = new Uri(grpcConfig.Configurator))
        .AddCallCredentials(CallCredentialsExtensions.GetAuthorizationToken)
        .ConfigureChannel(options => options.UnsafeUseInsecureChannelCallCredentials = true);
}

#endregion

#region App Builder

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<WebhookPayloadReceiverService>();
app.Run();

#endregion