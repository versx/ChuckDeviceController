namespace ChuckProxy.Extensions;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;

public static class ForwardContextExtensions
{
    public const string XForwardedFor = "X-Forwarded-For";
    public const string XForwardedHost = "X-Forwarded-Host";
    public const string XForwardedProto = "X-Forwarded-Proto";
    public const string XForwardedPathBase = "X-Forwarded-PathBase";

    /// <summary>
    ///     Adds X-Forwarded-For, X-Forwarded-Host, X-Forwarded-Proto and
    ///     X-Forwarded-PathBase headers to the forward request context. If
    ///     the headers already exist they will be appended otherwise they
    ///     will be added.
    /// </summary>
    /// <param name="forwardContext">The forward context.</param>
    /// <returns>The forward context.</returns>
    public static ForwardContext AddXForwardedHeaders(this ForwardContext forwardContext)
    {
        var headers = forwardContext.UpstreamRequest.Headers;
        var protocol = forwardContext.HttpContext.Request.Scheme;
        var @for = forwardContext.HttpContext.Connection.RemoteIpAddress;
        var host = forwardContext.HttpContext.Request.Headers["Host"];
        var hostString = HostString.FromUriComponent(host);
        var pathBase = forwardContext.HttpContext.Request.PathBase.Value;

        headers.ApplyXForwardedHeaders(@for, hostString, protocol, pathBase);

        return forwardContext;
    }

    /// <summary>
    ///     Applies X-Forwarded-* headers to the outgoing header collection
    ///     with an additional PathBase parameter.
    /// </summary>
    /// <param name="outgoingHeaders">The outgoing HTTP request
    /// headers.</param>
    /// <param name="for">The client IP address.</param>
    /// <param name="host">The host of the request.</param>
    /// <param name="proto">The protocol of the incoming request.</param>
    /// <param name="pathBase">The base path of the incoming
    /// request.</param>
    public static void ApplyXForwardedHeaders(
        this HttpRequestHeaders outgoingHeaders,
        IPAddress @for,
        HostString host,
        string proto,
        PathString pathBase)
    {
        if (@for != null)
        {
            var forString = @for.AddressFamily == AddressFamily.InterNetworkV6
                ? $"\"[{@for}]\""
                : @for.ToString();
            outgoingHeaders.Add(XForwardedFor, forString);
        }

        if (host.HasValue)
        {
            outgoingHeaders.Add(XForwardedHost, host.Value);
        }

        if (!string.IsNullOrWhiteSpace(proto))
        {
            outgoingHeaders.Add(XForwardedProto, proto);
        }

        if (!string.IsNullOrWhiteSpace(pathBase))
        {
            outgoingHeaders.Add(XForwardedPathBase, pathBase);
        }
    }
}