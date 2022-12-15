namespace ChuckDeviceController.Extensions.Http
{
    using System.Text;

    using Microsoft.AspNetCore.Http;

    public static partial class HttpRequestExtensions
    {
        // Credits: https://stackoverflow.com/a/14536035
        private static readonly IReadOnlyList<string> CrawlerKeywords = new List<string>
        {
            "bot", "crawler", "spider", "80legs", "baidu", "yahoo! slurp", "ia_archiver", "mediapartners-google",
            "lwp-trivial", "nederland.zoek", "ahoy", "anthill", "appie", "arale", "araneo", "ariadne",
            "atn_worldwide", "atomz", "bjaaland", "ukonline", "calif", "combine", "cosmos", "cusco",
            "cyberspyder", "digger", "grabber", "downloadexpress", "ecollector", "ebiness", "esculapio",
            "esther", "felix ide", "hamahakki", "kit-fireball", "fouineur", "freecrawl", "desertrealm",
            "gcreep", "golem", "griffon", "gromit", "gulliver", "gulper", "whowhere", "havindex", "hotwired",
            "htdig", "ingrid", "informant", "inspectorwww", "iron33", "teoma", "ask jeeves", "jeeves",
            "image.kapsi.net", "kdd-explorer", "label-grabber", "larbin", "linkidator", "linkwalker",
            "lockon", "marvin", "mattie", "mediafox", "merzscope", "nec-meshexplorer", "udmsearch", "moget",
            "motor", "muncher", "muninn", "muscatferret", "mwdsearch", "sharp-info-agent", "webmechanic",
            "netscoop", "newscan-online", "objectssearch", "orbsearch", "packrat", "pageboy", "parasite",
            "patric", "pegasus", "phpdig", "piltdownman", "pimptrain", "plumtreewebaccessor", "getterrobo-plus",
            "raven", "roadrunner", "robbie", "robocrawl", "robofox", "webbandit", "scooter", "search-au",
            "searchprocess", "senrigan", "shagseeker", "site valet", "skymob", "slurp", "snooper", "speedy",
            "curl_image_client", "suke", "www.sygol.com", "tach_bw", "templeton", "titin", "topiclink", "udmsearch",
            "urlck", "valkyrie libwww-perl", "verticrawl", "victoria", "webscout", "voyager", "crawlpaper",
            "webcatcher", "t-h-u-n-d-e-r-s-t-o-n-e", "webmoose", "pagesinventory", "webquest", "webreaper",
            "webwalker", "winona", "occam", "robi", "fdse", "jobo", "rhcs", "gazz", "dwcp", "yeti", "fido", "wlm",
            "wolp", "wwwc", "xget", "legs", "curl", "webs", "wget", "sift", "cmc",
        };

        public static bool IsDeclaredBotCrawler(this HttpRequest request)
        {
            var userAgent = request.GetUserAgent(toLower: true);
            var isBot = CrawlerKeywords.Any(keywords => userAgent.Contains(keywords));
            return isBot;
        }

        public static string GetUserAgent(this HttpRequest request, bool toLower = false)
        {
            var userAgent = request.Headers["User-Agent"].ToString();
            return toLower
                ? userAgent.ToLower()
                : userAgent;
        }

        public static string? GetIPAddress(this HttpRequest request, string? defaultValue = "0.0.0.0")
        {
            var cfHeader = request.Headers["CF-Connecting-IP"].ToString();
            var forwardedHost = request.Headers["X-Forwarded-Host"].ToString();
            var forwardedFor = request.Headers["X-Forwarded-For"].ToString()?.Split(',').FirstOrDefault();
            var remoteIp = request.HttpContext.Connection.RemoteIpAddress?.ToString();
            var localIp = request.HttpContext.Connection.LocalIpAddress?.ToString();
            var ipAddr = !string.IsNullOrEmpty(cfHeader)
                ? cfHeader
                : !string.IsNullOrEmpty(forwardedHost)
                    ? forwardedHost
                    : !string.IsNullOrEmpty(forwardedFor)
                        ? forwardedFor
                        : !string.IsNullOrEmpty(remoteIp)
                            ? remoteIp
                            : !string.IsNullOrEmpty(localIp)
                                ? localIp
                                : defaultValue;
            return ipAddr;
        }

        public static async Task<string> ReadBodyAsStringAsync(this HttpRequest request, Encoding? encoding = null)
        {
            try
            {
                request.EnableBuffering();
                using var stream = new StreamReader(request.Body, encoding ?? Encoding.UTF8);
                var data = await stream.ReadToEndAsync().ConfigureAwait(false);
                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error - ReadBodyAsStringAsync: {ex.Message}");
                return string.Empty;
            }
        }

        public static string? GetHeader(this HttpRequest request, string key)
        {
            if (!request.Headers.ContainsKey(key))
                return null;

            var header = request.Headers[key].ToString();
            return header;
        }

        public static string? GetAuthorizationHeader(this HttpRequest request, bool removePrefix = true)
        {
            var header = request.GetHeader("Authorization");
            if (removePrefix)
            {
                header = header?.Replace("Bearer ", null);
            }
            return header?.Replace("\"", null);
        }
    }
}