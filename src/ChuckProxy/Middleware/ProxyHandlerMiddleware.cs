namespace ChuckProxy.Middleware;

/// <summary>
/// Credits: https://github.com/ProxyKit/ProxyKit
/// </summary>
public class ProxyHandlerMiddleware<TProxyHandler>
    where TProxyHandler : IProxyHandler
{
    private readonly TProxyHandler _handler;
    private const int StreamCopyBufferSize = 81920;

    public ProxyHandlerMiddleware(RequestDelegate _, TProxyHandler handler)
    {
        _handler = handler;
    }

    public async Task Invoke(HttpContext context)
    {
        using var response = await _handler.HandleProxyRequest(context).ConfigureAwait(false);
        await CopyProxyHttpResponse(context, response).ConfigureAwait(false);
    }

    private static async Task CopyProxyHttpResponse(HttpContext context, HttpResponseMessage responseMessage)
    {
        var response = context.Response;

        response.StatusCode = (int)responseMessage.StatusCode;
        foreach (var header in responseMessage.Headers)
        {
            response.Headers[header.Key] = header.Value.ToArray();
        }

        if (responseMessage.Content != null)
        {
            foreach (var header in responseMessage.Content.Headers)
            {
                response.Headers[header.Key] = header.Value.ToArray();
            }
        }

        // SendAsync removes chunking from the response. This removes the header so it doesn't expect a chunked response.
        response.Headers.Remove("transfer-encoding");

        if (responseMessage.Content != null)
        {
            using var responseStream = await responseMessage
                .Content
                .ReadAsStreamAsync()
                .ConfigureAwait(false);
            try
            {
                await responseStream
                    .CopyToAsync(response.Body, StreamCopyBufferSize, context.RequestAborted)
                    .ConfigureAwait(false);
                if (responseStream.CanWrite)
                {
                    await responseStream
                        .FlushAsync(context.RequestAborted)
                        .ConfigureAwait(false);
                }
            }
            catch (IOException ex)
            {
                // Usually a client abort. Ignore.
                Console.WriteLine($"[CopyProxyHttpResponse] Error: {ex}");
            }
        }
    }
}