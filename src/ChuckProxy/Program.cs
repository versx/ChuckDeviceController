using ChuckProxy;
using ChuckProxy.Configuration;
using ChuckProxy.Extensions;

#region Configuration

var config = Config.LoadConfig(args, Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
if (config.Providers.Count() == 2)
{
    // Only environment variables and command line providers added,
    // failed to load config provider.
    Environment.FailFast($"Failed to find or load configuration file, exiting...");
}
var proxyConfig = new ProxyConfig();
config.Bind(proxyConfig);
if (proxyConfig == null)
{
    Environment.FailFast($"Failed to load proxy configuration file, exiting...");
    return;
}

#endregion

#region Services

var logger = new Logger<Program>(LoggerFactory.Create(x => x.AddConsole()));
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseConfiguration(config);
builder.WebHost.UseUrls(proxyConfig.Urls);

builder.Services
    .AddHttpClient(Strings.ProxyHttpClientName)
    .ConfigurePrimaryHttpMessageHandler(sp => new HttpClientHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false,
    });

#endregion

#region Application Builder

var app = builder.Build();
#if DEBUG
app.MapGet("/", () => ":D");
#endif

if (proxyConfig?.RawEndpoints?.Any() ?? false)
{
    app.UseWhen(
        context => Strings.RawEndpoint == context.Request.Path.ToString(),
        HandleProxiedEndpoint(proxyConfig.RawEndpoints.First())
        //HandleProxiedEndpoints(proxyConfig.RawEndpoints)
    );
}
if (!string.IsNullOrEmpty(proxyConfig?.ControllerEndpoint))
{
    app.UseWhen(
        context => Strings.ControllerEndpoints.Contains(context.Request.Path.ToString()),
        HandleProxiedEndpoint(proxyConfig.ControllerEndpoint)
    );
}

PrintHeader();
await app.RunAsync();

#endregion

#region Helpers

Action<IApplicationBuilder> HandleProxiedEndpoint(string endpoint)
{
    var proxyHandler = (IApplicationBuilder appProxy) =>
        appProxy.RunProxy(context => context
            .ForwardTo(endpoint)
            .AddXForwardedHeaders()
            .Send());
    return proxyHandler;
}

void PrintHeader()
{
    logger.LogInformation($"Proxy relay server is listening at {proxyConfig!.Urls}");
    logger.LogInformation($"All requests to endpoint '/raw' will be proxied to the following:\n\t - {string.Join("\n\t - ", proxyConfig!.RawEndpoints)}");
    logger.LogInformation($"All requests to endpoint '/controler' and '/controller' will be proxied to the following:\n\t - {proxyConfig!.ControllerEndpoint}");
}

#endregion