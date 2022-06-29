namespace ChuckDeviceController.Data.Entities
{
    using System.Text.Json.Serialization;

    public class InstanceData
    {
        [JsonPropertyName("circle_route_type")]
        public CircleRouteType CircleRouteType { get; set; }

        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }

        [JsonPropertyName("enable_dst")]
        public bool? EnableDst { get; set; }

        [JsonPropertyName("iv_queue_limit")]
        public ushort? IVQueueLimit { get; set; }

        [JsonPropertyName("iv_list")]
        public string? IVList { get; set; }

        [JsonPropertyName("spin_limit")]
        public ushort? SpinLimit { get; set; }

        [JsonPropertyName("circle_size")]
        public ushort? CircleSize { get; set; }

        [JsonPropertyName("account_group")]
        public string AccountGroup { get; set; }

        [JsonPropertyName("is_event")]
        public bool IsEvent { get; set; }

        public InstanceData()
        {
            CircleRouteType = CircleRouteType.Default;
        }
    }
}