namespace ChuckDeviceController.Authorization.Jwt.Extensions
{
    using System.Net;

    using Grpc.Core;

    using ChuckDeviceController.Authorization.Jwt.Models;
    using ChuckDeviceController.Extensions.Json;
    using ChuckDeviceController.Net.Utilities;

    public static class CallCredentialsExtensions
    {
        public static async Task GetAuthorizationToken(AuthInterceptorContext context, Metadata metadata, IServiceProvider serviceProvider)
        {
            // TODO: Make Jwt endpoint configurable
            var url = "http://127.0.0.1:8881/api/jwt/generate?identifier=Grpc";
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
}