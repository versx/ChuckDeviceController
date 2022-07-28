using ChuckDeviceCommunicator.Services;
using ChuckDeviceCommunicator.Services.Rpc;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddSingleton<IWebhookRelayService, WebhookRelayService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<WebhookPayloadReceiverService>();

app.Run();