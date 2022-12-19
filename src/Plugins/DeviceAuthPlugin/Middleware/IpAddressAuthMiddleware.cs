namespace DeviceAuthPlugin.Middleware;

using System.Net;

using Microsoft.Extensions.Options;

using Configuration;
using Extensions;

using ChuckDeviceController.Extensions.Http;

public sealed class IpAddressAuthMiddleware
{
    private static readonly IEnumerable<string> AffectedRoutes = new List<string>
    {
        "/controller",
        "/controler",
        "/raw",
    };
    private readonly RequestDelegate _next;

    public IpAuthConfig Options { get; }

    public IpAddressAuthMiddleware(
        RequestDelegate next,
        IOptions<IpAuthConfig> optionsAccessor)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        Options = optionsAccessor.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.ToString().ToLower();
        if (!AffectedRoutes.Any(route => route == path))
        {
            await _next(context);
            return;
        }

        // Deny all requests if no IP addresses set but middleware is enabled
        if (!Options.IpAddresses.Any())
        {
            return;
        }

        var ipAddress = context.Request.GetIPAddress();
        if (!IsValid(ipAddress, Options.IpAddresses))
        {
            return;
        }

        await _next(context);
    }

    private static bool IsValid(string ipAddress, IEnumerable<string> ipAddresses)
    {
        var result = false;
        var ipAddr = IPAddress.Parse(ipAddress);
        foreach (var entry in ipAddresses)
        {
            if (entry.Contains('-'))
            {
                // Check if within IP range
                var split = entry.Split('-');
                if (split.Length != 2)
                    continue;

                var rangeStart = IPAddress.Parse(split[0]);
                var rangeEnd = IPAddress.Parse(split[1]);
                var isInRange = ipAddr.IsInRange(rangeStart, rangeEnd);
                if (isInRange)
                {
                    result = true;
                    break;
                }
            }
            else if (entry.Contains('/'))
            {
                // Check if IP in subnet
                var isInSubnet = ipAddr.IsInSubnet(entry);
                if (isInSubnet)
                {
                    result = true;
                    break;
                }
            }
            else
            {
                // Check if explicit match
                if (ipAddress == entry)
                {
                    result = true;
                    break;
                }
            }
        }

        return result;
    }
}