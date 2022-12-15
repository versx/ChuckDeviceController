namespace ChuckProxy.Extensions;

using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Net.Http.Headers;

public static class HttpContextExtensions
{
    /// <summary>
    ///     Forward the request to the specified upstream host.
    /// </summary>
    /// <param name="context">The HttpContext</param>
    /// <param name="upstreamHost">The upstream host to forward the requests
    /// to.</param>
    /// <returns>A <see cref="ForwardContext"/> that represents the
    /// forwarding request context.</returns>
    public static ForwardContext ForwardTo(this HttpContext context, UpstreamHost upstreamHost)
    {
        var uri = new Uri(UriHelper.BuildAbsolute(
            upstreamHost.Scheme,
            upstreamHost.Host,
            upstreamHost.PathBase,
            //context.Request.Path,
            "",
            context.Request.QueryString
        ));

        var request = context.Request.CreateProxyHttpRequest();
        request.Headers.Host = uri.Authority;
        request.RequestUri = uri;

        IHttpClientFactory httpClientFactory;
        try
        {
            httpClientFactory = context
                .RequestServices
                .GetRequiredService<IHttpClientFactory>();
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException($"{ex.Message} Did you forget to call services.AddProxy()?", ex);
        }

        var httpClient = httpClientFactory.CreateClient(Strings.ProxyHttpClientName);

        return new ForwardContext(httpClient, context, request);
    }

    private static HttpRequestMessage CreateProxyHttpRequest(this HttpRequest request)
    {
        var requestMessage = new HttpRequestMessage();

        // The presence of a message-body in a request is signaled by the
        // inclusion of a Content-Length or Transfer-Encoding header field in
        // the request's message-headers. https://tools.ietf.org/html/rfc2616 4.3 MessageBody
        if (request.ContentLength > 0 || request.Headers.ContainsKey("Transfer-Encoding"))
        {
            requestMessage.Content = new StreamContent(request.Body);
        }

        // Copy the request headers *except* x-forwarded-* headers.
        foreach (var header in request.Headers)
        {
            if (header.Key.StartsWith("X-Forwarded-", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var headerName = header.Key;
            var value = header.Value;

            if (string.Equals(headerName, HeaderNames.Cookie, StringComparison.OrdinalIgnoreCase) && value.Count > 1)
            {
                value = string.Join("; ", value);
            }

            if (value.Count == 1)
            {
                string headerValue = value;
                if (!requestMessage.Headers.TryAddWithoutValidation(headerName, headerValue))
                {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(headerName, headerValue);
                }
            }
            else
            {
                string[] headerValues = value;
                if (!requestMessage.Headers.TryAddWithoutValidation(headerName, headerValues))
                {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(headerName, headerValues);
                }
            }
        }

        requestMessage.Method = new HttpMethod(request.Method);
        return requestMessage;
    }
}