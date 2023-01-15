namespace ChuckDeviceController.Authorization.Jwt.Extensions;

using System.Net;

using Grpc.Core;
using Microsoft.Extensions.Configuration;

using ChuckDeviceController.Authorization.Jwt.Models;
using ChuckDeviceController.Extensions.Json;
using ChuckDeviceController.Net.Utilities;

public static class CallCredentialsExtensions
{
#pragma warning disable IDE0060 // Remove unused parameter
    public static async Task GetAuthorizationToken(AuthInterceptorContext context, Metadata metadata, IServiceProvider serviceProvider)
#pragma warning restore IDE0060 // Remove unused parameter
    {
        var config = (IConfiguration?)serviceProvider.GetService(typeof(IConfiguration));
        var host = config?.GetValue<string>("ConfiguratorUrl") ?? Strings.DefaultApiEndpoint;
        var url = host + Strings.JwtEndpoint;

        // Send HTTP request to fetch json web token to authenticate gRPC request
        var (status, json) = await NetUtils.PostAsync(url);
        if (status != HttpStatusCode.OK || json == null)
        {
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