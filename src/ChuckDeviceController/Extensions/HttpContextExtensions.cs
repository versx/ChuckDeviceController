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

        public static bool IsMadDeviceRequest(this HttpContext context, string rawDataEndpoint)
        {
            // We only care about 'POST' requests
            if (context.Request.Method.ToUpper() != "POST")
                return false;

            // Check if request is to '/raw' endpoint
            var path = context.Request.Path.ToString().ToLower();
            if (path != rawDataEndpoint)
                return false;

            // Check if 'Origin' header is set, which MAD sets as device id
            var isMad = context.Request.Headers.ContainsKey(OriginHeader);
            return isMad;
        }

        public static async Task ConvertPayloadDataAsync(this HttpContext context, string username)
        {
            // TODO: Add TryCatch
            // At this point this should be a MAD device sending the request.
            // Read the request's post body.
            var uuid = context.Request.Headers[OriginHeader];
            var data = await context.Request.ReadBodyAsStringAsync();
            if (string.IsNullOrEmpty(data))
                return;

            var contents = data.FromJson<List<ProtoData>>();
            var payload = new ProtoPayload
            {
                Contents = contents,
                Username = username,
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
    }
}