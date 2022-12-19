namespace ChuckDeviceController.Http.Proxy.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

using ChuckDeviceController.Http.Proxy.Middleware;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    ///     Runs a reverse proxy forwarding requests to an upstream host.
    /// </summary>
    /// <param name="app">
    ///     The application builder.
    /// </param>
    /// <param name="handleProxyRequest">
    ///     A delegate that can resolve the destination Uri.
    /// </param>
    public static void RunProxy(this IApplicationBuilder app, HandleProxyRequest handleProxyRequest)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        if (handleProxyRequest == null)
        {
            throw new ArgumentNullException(nameof(handleProxyRequest));
        }

        app.UseMiddleware<ProxyHandlerMiddleware<HandleProxyRequestWrapper>>(new HandleProxyRequestWrapper(handleProxyRequest));
    }

    private class HandleProxyRequestWrapper : IProxyHandler
    {
        private readonly HandleProxyRequest _handleProxyRequest;

        public HandleProxyRequestWrapper(HandleProxyRequest handleProxyRequest)
        {
            _handleProxyRequest = handleProxyRequest;
        }

        public Task<HttpResponseMessage> HandleProxyRequest(HttpContext httpContext) =>
            _handleProxyRequest(httpContext);
    }
}