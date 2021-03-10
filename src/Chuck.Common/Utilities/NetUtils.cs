namespace Chuck.Common.Utilities
{
    using System;
    using System.Net;
    using System.Threading;

    public static class NetUtils
    {
        private const ushort MaxRetryCount = 3;

        /// <summary>
        /// Sends webhook data
        /// </summary>
        /// <param name="webhookUrl"></param>
        /// <param name="json"></param>
        public static void SendWebhook(string webhookUrl, string json, double delay = 5, ushort retryCount = 0)
        {
            if (retryCount >= MaxRetryCount)
                return;

            using var wc = new WebClient();
            wc.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            try
            {
                var resp = wc.UploadString(webhookUrl, json);
                Thread.Sleep(Convert.ToInt32(delay * 1000));
            }
            catch (WebException ex)
            {
                var resp = (HttpWebResponse)ex.Response;
                if (resp == null)
                {
                    Thread.Sleep(retryCount);
                    retryCount++;
                    SendWebhook(webhookUrl, json, delay, retryCount);
                    return;
                }
                switch ((int)resp.StatusCode)
                {
                    case 429:
                        Console.WriteLine("RATE LIMITED");
                        var retryAfter = resp.Headers["Retry-After"];
                        if (!int.TryParse(retryAfter, out var retry))
                            return;

                        Thread.Sleep(retry);
                        retryCount++;
                        SendWebhook(webhookUrl, json, delay * 1000, retryCount);
                        break;
                }
            }
        }
    }
}