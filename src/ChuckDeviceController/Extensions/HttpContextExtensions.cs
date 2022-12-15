namespace ChuckDeviceController.Extensions
{
    using System.Text;

    using ChuckDeviceController.Extensions.Http;
    using ChuckDeviceController.Extensions.Json;
    using ChuckDeviceController.Net.Models.Requests;

    public static class HttpContextExtensions
    {
        private const string OriginHeader = "Origin";
        private const string JsonContentType = "application/json";
        private const string DefaultMadUsername = "PogoDroid";
        private const string RawDataEndpoint = "/raw";
        private const string PostMethod = "POST";

        public static async Task ConvertPayloadDataAsync(this HttpContext context)
        {
            try
            {
                // At this point this should be a MAD device sending the request,
                // retrieve the account username from the Origin HTTP header.
                var uuid = context.Request.Headers[OriginHeader];

                // Read the request's post body as a string.
                var data = await context.Request.ReadBodyAsStringAsync();
                if (string.IsNullOrEmpty(data))
                    return;

                var contents = data.FromJson<List<ProtoData>>();
                var payload = new ProtoPayload
                {
                    Contents = contents,
                    Username = DefaultMadUsername,
                    Uuid = uuid,
                };

                // Replace request stream to downstream handlers
                var json = payload.ToJson();
                //var requestData = Encoding.UTF8.GetBytes(json);
                //context.Request.Body = new MemoryStream(requestData);
                var content = new StringContent(json, Encoding.UTF8, JsonContentType);
                context.Request.Body = await content.ReadAsStreamAsync();
                context.Request.ContentLength = context.Request.Body.Length;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ConvertPayloadDataAsync: {ex}");
            }
        }

        public static bool IsPostRequest(this HttpContext context)
        {
            return context.Request.Method.ToUpper() == PostMethod;
        }

        public static bool IsRawDataRequest(this HttpContext context)
        {
            return context.Request.Path.StartsWithSegments(RawDataEndpoint);
        }

        public static bool IsOriginHeaderSet(this HttpContext context)
        {
            return context.Request.Headers.ContainsKey(OriginHeader);
        }
    }
}