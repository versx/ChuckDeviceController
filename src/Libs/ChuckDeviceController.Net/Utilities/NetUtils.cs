namespace ChuckDeviceController.Net.Utilities;

using System.Net;
using System.Text;

public static class NetUtils
{
    public const string DefaultUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/106.0.0.0 Safari/537.36";
    public const string DefaultMimeType = "application/json";
    public const uint DefaultRequestTimeoutS = 15;

    public const string AcceptHeader = "Accept";
    public const string ContentTypeHeader = "Content-Type";
    public const string HostHeader = "Host";
    public const string UserAgentHeader = "User-Agent";

    public static string? Get(string url, uint timeoutS = DefaultRequestTimeoutS)
    {
        return GetAsync(url, timeoutS).Result;
    }

    /// <summary>
    /// Sends a HTTP GET request to the specified url.
    /// </summary>
    /// <param name="url">Url to send the request to.</param>
    /// <param name="timeoutS">Maximum time to wait for request before aborting.</param>
    /// <returns>Returns the response string of the HTTP GET request.</returns>
    public static async Task<string?> GetAsync(string url, uint timeoutS = DefaultRequestTimeoutS)
    {
        try
        {
            SetDefaultSecurityProtocol();

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add(AcceptHeader, DefaultMimeType);
            client.DefaultRequestHeaders.Add(UserAgentHeader, DefaultUserAgent);
            client.Timeout = TimeSpan.FromSeconds(timeoutS);
            return await client.GetStringAsync(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to download data from {url}: {ex.Message}");
        }
        return null;
    }

    public static (HttpStatusCode, string?) Post(string url, string? payload = null)
    {
        return PostAsync(url, payload).Result;
    }

    /// <summary>
    /// Sends a HTTP POST request to the specified url with JSON payload.
    /// </summary>
    /// <param name="url">Url to send the request to.</param>
    /// <param name="payload">JSON payload that will be sent in the request.</param>
    /// <param name="timeoutS"></param>
    /// <param name="userAgent"></param>
    /// <returns>Returns the response string of the HTTP POST request.</returns>
    public static async Task<(HttpStatusCode, string?)> PostAsync(string url, string? payload = null, uint timeoutS = DefaultRequestTimeoutS, string? userAgent = DefaultUserAgent)
    {
        try
        {
            SetDefaultSecurityProtocol();

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(timeoutS);
            var requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url),
                Headers =
                {
                    { AcceptHeader, DefaultMimeType },
                    { UserAgentHeader, userAgent ?? DefaultUserAgent },
                },
                Content = new StringContent(payload ?? string.Empty, Encoding.UTF8, DefaultMimeType),
            };

            var response = await client.SendAsync(requestMessage);
            var responseData = await response.Content.ReadAsStringAsync();
            return (response.StatusCode, responseData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to post data to {url}: {ex.Message}");
        }
        return (HttpStatusCode.BadRequest, null);
    }

    public static HttpResponseMessage? Head(string url, uint timeoutS = DefaultRequestTimeoutS)
    {
        return HeadAsync(url, timeoutS).Result;
    }

    public static async Task<HttpResponseMessage?> HeadAsync(string url, uint timeoutS = DefaultRequestTimeoutS)
    {
        try
        {
            SetDefaultSecurityProtocol();

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(timeoutS);
            var requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Head,
                RequestUri = new Uri(url),
                Headers =
                {
                    //{ AcceptHeader, DefaultMimeType },
                    { UserAgentHeader, DefaultUserAgent },
                },
            };
            var response = await client.SendAsync(requestMessage);
            //var responseData = await response.Content.ReadAsStringAsync();
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to download data from {url}: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// Fixes Exception: Authentication failed because the remote party sent a TLS alert: 'DecryptError'.
    /// </summary>
    private static void SetDefaultSecurityProtocol()
    {
        // Credits: https://stackoverflow.com/a/35321007
        ServicePointManager.SecurityProtocol =
            SecurityProtocolType.Tls12 |
            SecurityProtocolType.Tls11 |
            SecurityProtocolType.Tls;
    }
}