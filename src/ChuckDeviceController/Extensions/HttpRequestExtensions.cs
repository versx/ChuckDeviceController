namespace ChuckDeviceController.Extensions
{
    using System;
    using System.Linq;

    using Microsoft.AspNetCore.Http;

    public static class HttpRequestExtensions
    {
        public static string GetIPAddress(this HttpRequest request)
        {
            var cfHeader = request.Headers["cf-connecting-ip"].ToString();
            var forwardedfor = request.Headers["x-forwarded-for"].ToString()?.Split(",").FirstOrDefault();
            var remoteIp = request.HttpContext.Connection.RemoteIpAddress?.ToString();
            var localIp = request.HttpContext.Connection.LocalIpAddress?.ToString();
            var ipAddr = !string.IsNullOrEmpty(cfHeader)
                ? cfHeader
                : !string.IsNullOrEmpty(forwardedfor)
                    ? forwardedfor
                    : !string.IsNullOrEmpty(remoteIp)
                        ? remoteIp
                        : !string.IsNullOrEmpty(localIp)
                            ? localIp
                            : string.Empty;
            return ipAddr;
        }
    }
}