namespace ChuckDeviceController.Authorization.Jwt.Extensions;

using System.Net;

using Grpc.Core;
using Microsoft.Extensions.Configuration;

using ChuckDeviceController.Authorization.Jwt.Models;
using ChuckDeviceController.Extensions.Json;
using ChuckDeviceController.Net.Utilities;

public static class CallCredentialsExtensions
{
    private const string DefaultEndpoint = "http://127.0.0.1:8881";

    public static async Task GetAuthorizationToken(AuthInterceptorContext context, Metadata metadata, IServiceProvider serviceProvider)
    {
        var config = (IConfiguration?)serviceProvider.GetService(typeof(IConfiguration));
        var host = config?.GetValue<string>("ConfiguratorUrl") ?? DefaultEndpoint;
        var url = host + Strings.JwtEndpoint;
        var (status, json) = await NetUtils.PostAsync(url);
        if (status != HttpStatusCode.OK || json == null)
        {
            Console.WriteLine($"Failed to send request for JWT");
            return;
        }

        var response = json?.FromJson<JwtResponse>() ?? new();
        var token = response?.AccessToken;
        if (token != null)
        {
            metadata.Add(Strings.AuthorizationHeader, $"Bearer {token}");
        }
    }
}