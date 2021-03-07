namespace ChuckDeviceController.Utilities
{
    using System;
    using System.Net;
    using System.Threading;

    using ChuckDeviceController.Extensions;

    public static class NetUtil
    {
        private const ushort MaxRetryCount = 3;

        /// <summary>
        /// Sends webhook data
        /// </summary>
        /// <param name="webhookUrl"></param>
        /// <param name="json"></param>
        public static void SendWebhook(string webhookUrl, string json, ushort sleep = 50, ushort retryCount = 0)
        {
            if (retryCount >= MaxRetryCount)
                return;

            using var wc = new WebClient();
            wc.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            try
            {
                var resp = wc.UploadString(webhookUrl, json);
                Thread.Sleep(sleep);
            }
            catch (WebException ex)
            {
                var resp = (HttpWebResponse)ex.Response;
                switch ((int)resp.StatusCode)
                {
                    case 429:
                        ConsoleExt.WriteWarn("RATE LIMITED");
                        var retryAfter = resp.Headers["Retry-After"];
                        if (!int.TryParse(retryAfter, out var retry))
                            return;

                        Thread.Sleep(retry);
                        retryCount++;
                        SendWebhook(webhookUrl, json, sleep, retryCount);
                        break;
                }
            }
        }
    }
}