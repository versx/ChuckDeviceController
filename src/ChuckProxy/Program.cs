using ChuckDeviceController.Configuration;
using ChuckDeviceController.Http.Proxy.Configuration;
using ChuckDeviceController.Http.Proxy.Extensions;
using ChuckProxy;

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
    .AddHttpClient(HttpContextExtensions.DefaultProxyHttpClientName, options =>
    {
        options.Timeout = TimeSpan.FromSeconds(10);
    })
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

//app.Use(HandleProxiedRequest);
if (proxyConfig?.RawEndpoints?.Any() ?? false)
{
    app.UseWhen(
        context => Strings.RawEndpoint == context.Request.Path.ToString(),
        HandleProxiedEndpoint(proxyConfig.RawEndpoints.First())
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

//async Task HandleProxiedRequest(HttpContext context, RequestDelegate next)
//{
//    var path = context.Request.Path.ToString();
//    if (Strings.RawEndpoint != path)
//        return;

//    var request = context.Request.CreateProxyHttpRequest();
//    switch (path)
//    {
//        case Strings.RawEndpoint:
//            var rawUri = new Uri(proxyConfig.RawEndpoints.First());
//            request.Headers.Host = rawUri.Authority;
//            request.RequestUri = rawUri;
//            break;
//    }

//    try
//    {
//        var httpClientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
//        using var httpClient = httpClientFactory.CreateClient(Strings.ProxyHttpClientName);
//        var response = await httpClient.SendAsync(request);//, HttpCompletionOption.ResponseHeadersRead);
//        Console.WriteLine($"Status: {response.StatusCode}, Reason: {response.ReasonPhrase}");

//        //var forwardContext = new ForwardContext(httpClient, context, request).AddXForwardedHeaders();
//        //await forwardContext.Send();
//    }
//    catch (InvalidOperationException ex)
//    {
//        throw new InvalidOperationException($"{ex.Message} Did you forget to call services.AddProxy()?", ex);
//    }
//}

Action<IApplicationBuilder> HandleProxiedEndpoint(string endpoint)
{
    var proxyHandler = (IApplicationBuilder appProxy) =>
        appProxy.RunProxy(async context =>
        {
            var response = await context
                .ForwardTo(endpoint)
                .AddXForwardedHeaders()
                .Send();
            logger.LogDebug($"Status: {response.StatusCode}, Reason: {response.ReasonPhrase}");
            return response;
        });
    return proxyHandler;
}

void PrintHeader()
{
    logger.LogInformation($"Proxy relay server is listening at {proxyConfig!.Urls}");
    logger.LogInformation($"All requests to endpoint '/raw' will be proxied to the following:\n\t - {string.Join("\n\t - ", proxyConfig!.RawEndpoints)}");
    logger.LogInformation($"All requests to endpoint '/controler' and '/controller' will be proxied to the following:\n\t - {proxyConfig!.ControllerEndpoint}");
}

#endregion