namespace ChuckDeviceController.Net.Models.Requests.Koji;

using System.Text.Json.Serialization;

public class KojiBootstrapRequest : BaseKojiRequest
{
    [JsonPropertyName("return_type")]
    public double? Radius { get; set; } // optional
}