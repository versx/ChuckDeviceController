namespace Chuck.Infrastructure.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    using Chuck.Infrastructure.Common;

    public class InstanceData
    {
        [JsonPropertyName("circle_route_type")]
        public CircleRouteType CircleRouteType { get; set; }

        [JsonPropertyName("min_level")]
        public ushort MinimumLevel { get; set; }

        [JsonPropertyName("max_level")]
        public ushort MaximumLevel { get; set; }

        [JsonPropertyName("timezone")]
        public string Timezone { get; set; }

        [JsonPropertyName("enable_dst")]
        public bool EnableDst { get; set; }

        //[JsonPropertyName("area")]
        //public dynamic Area { get; set; }

        [JsonPropertyName("iv_queue_limit")]
        public ushort? IVQueueLimit { get; set; }

        [JsonPropertyName("pokemon_ids")]
        public List<uint> PokemonIds { get; set; }

        [JsonPropertyName("spin_limit")]
        public ushort? SpinLimit { get; set; }

        [JsonPropertyName("circle_size")]
        public ushort? CircleSize { get; set; }

        [JsonPropertyName("fast_bootstrap_mode")]
        public bool FastBootstrapMode { get; set; }

        [JsonPropertyName("account_group")]
        public string AccountGroup { get; set; }

        [JsonPropertyName("is_event")]
        public bool IsEvent { get; set; }

        [JsonPropertyName("quest_retry_limit")]
        public byte? QuestRetryLimit { get; set; }

        public InstanceData()
        {
            CircleRouteType = CircleRouteType.Default;
        }
    }
}