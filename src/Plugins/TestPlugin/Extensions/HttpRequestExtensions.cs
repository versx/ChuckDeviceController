namespace TestPlugin.Extensions
{
    using System.Text;

    using Microsoft.AspNetCore.Http;

    public static class HttpRequestExtensions
    {
        public static async Task<string> ReadBodyAsStringAsync(this HttpRequest request, Encoding? encoding = null)
        {
            if (!request.Body.CanSeek)
            {
                // We only do this if the stream isn't *already* seekable,
                // as EnableBuffering will create a new stream instance
                // each time it's called
                request.EnableBuffering();
            }

            // Ensure we read from the beginning of the stream
            request.Body.Position = 0;

            using var readStream = new StreamReader(request.Body, encoding ?? Encoding.UTF8);
            var bodyString = await readStream.ReadToEndAsync().ConfigureAwait(false);

            // Reset the stream position
            request.Body.Position = 0;
            return bodyString;
        }
    }
}