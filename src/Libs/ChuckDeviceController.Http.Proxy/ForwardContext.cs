namespace ChuckDeviceController.Http.Proxy;

using System.Net;
using System.Net.Sockets;

using Microsoft.AspNetCore.Http;

public class ForwardContext
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// The incoming HttpContext
    /// </summary>
    public HttpContext HttpContext { get; }

    /// <summary>
    /// The upstream request message.
    /// </summary>
    public HttpRequestMessage UpstreamRequest { get; }

    internal ForwardContext(
        HttpClient httpClient,
        HttpContext httpContext,
        HttpRequestMessage upstreamRequest)
    {
        _httpClient = httpClient;
        HttpContext = httpContext;
        UpstreamRequest = upstreamRequest;
    }

    /// <summary>
    /// Sends the upstream request to the upstream host.
    /// </summary>
    /// <returns>An <see cref="HttpResponseMessage"/> the represents the proxy response.</returns>
    public async Task<HttpResponseMessage> Send()
    {
        try
        {
            return await _httpClient
                .SendAsync(
                    UpstreamRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    HttpContext.RequestAborted)
                .ConfigureAwait(false);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is IOException)
        {
            return new HttpResponseMessage(HttpStatusCode.GatewayTimeout);
        }
        catch (OperationCanceledException)
        {
            // Happens when Timeout is low and upstream host is not reachable.
            return new HttpResponseMessage(HttpStatusCode.BadGateway);
        }
        catch (HttpRequestException ex)
            when (ex.InnerException is IOException || ex.InnerException is SocketException)
        {
            // Happens when server is not reachable
            return new HttpResponseMessage(HttpStatusCode.BadGateway);
        }
    }
}