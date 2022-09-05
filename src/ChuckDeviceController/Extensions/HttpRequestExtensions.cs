namespace ChuckDeviceController.Extensions
{
    using System.Text;

    public static class HttpRequestExtensions
    {
        public static async Task<string> ReadBodyAsStringAsync(this HttpRequest request, Encoding? encoding = null)
        {
            try
            {
                using var stream = new StreamReader(request.Body, encoding ?? Encoding.UTF8);
                var data = await stream.ReadToEndAsync().ConfigureAwait(false);
                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
                return string.Empty;
            }
        }
    }
}