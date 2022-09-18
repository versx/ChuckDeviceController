﻿namespace ChuckDeviceController.Net.Utilities
{
    using System.Net;
    using System.Text;

    public static class NetUtils
    {
        public const string DefaultUserAgent = "Mozilla/5.0";
        public const string DefaultMimeType = "application/json";
        private const uint DefaultRequestTimeoutS = 30;

        public static string? Get(string url)
        {
            return GetAsync(url).Result;
        }

        /// <summary>
        /// Sends a HTTP GET request to the specified url.
        /// </summary>
        /// <param name="url">Url to send the request to.</param>
        /// <returns>Returns the response string of the HTTP GET request.</returns>
        public static async Task<string?> GetAsync(string url)
        {
            try
            {
                SetDefaultSecurityProtocol();

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add(HttpRequestHeader.Accept.ToString(), DefaultMimeType);
                client.DefaultRequestHeaders.Add(HttpRequestHeader.ContentType.ToString(), DefaultMimeType);
                client.Timeout = TimeSpan.FromSeconds(DefaultRequestTimeoutS);
                return await client.GetStringAsync(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to download data from {url}: {ex}");
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
        /// <returns>Returns the response string of the HTTP POST request.</returns>
        public static async Task<(HttpStatusCode, string?)> PostAsync(string url, string? payload = null, uint timeoutS = DefaultRequestTimeoutS, string userAgent = DefaultUserAgent)
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
                        { HttpRequestHeader.Accept.ToString(), DefaultMimeType },
                        { HttpRequestHeader.ContentType.ToString(), DefaultMimeType },
                        { HttpRequestHeader.UserAgent.ToString(), userAgent ?? DefaultUserAgent },
                    },
                };
                if (!string.IsNullOrEmpty(payload))
                {
                    requestMessage.Content = new StringContent(payload, Encoding.UTF8, DefaultMimeType);
                }

                var response = await client.SendAsync(requestMessage);
                var responseData = await response.Content.ReadAsStringAsync();
                return (response.StatusCode, responseData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to post data to {url}: {ex}");
            }
            return (HttpStatusCode.BadRequest, null);
        }

        public static HttpResponseMessage? Head(string url)
        {
            return HeadAsync(url).Result;
        }

        public static async Task<HttpResponseMessage?> HeadAsync(string url)
        {
            try
            {
                SetDefaultSecurityProtocol();

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(DefaultRequestTimeoutS);
                var requestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Head,
                    RequestUri = new Uri(url),
                    Headers =
                    {
                        { HttpRequestHeader.Accept.ToString(), DefaultMimeType },
                        { HttpRequestHeader.ContentType.ToString(), DefaultMimeType },
                        { HttpRequestHeader.UserAgent.ToString(), DefaultUserAgent },
                    },
                    //Content = new StringContent(payload, Encoding.UTF8, DefaultMimeType),
                };
                var response = await client.SendAsync(requestMessage);
                //var responseData = await response.Content.ReadAsStringAsync();
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to download data from {url}: {ex}");
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
}