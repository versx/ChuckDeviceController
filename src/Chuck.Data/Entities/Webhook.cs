namespace Chuck.Data.Entities
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;
    using System.Text.Json.Serialization;

    using Chuck.Data.Interfaces;

    [Table("webhook")]
    public class Webhook : BaseEntity, IAggregateRoot
    {
        [
            Column("name"),
            Key,
            JsonPropertyName("name"),
        ]
        public string Name { get; set; }

        [
            Column("types"),
            JsonPropertyName("types"),
        ]
        public List<WebhookType> Types { get; set; }

        [
            Column("delay"),
            JsonPropertyName("delay"),
        ]
        public double Delay { get; set; }

        [
            Column("url"),
            JsonPropertyName("url"),
        ]
        public string Url { get; set; }

        [
            Column("enabled"),
            JsonPropertyName("enabled"),
        ]
        public bool Enabled { get; set; }

        [
            Column("geofences"),
            JsonPropertyName("geofences"),
        ]
        public List<string> Geofences { get; set; }

        [
            Column("data"),
            JsonPropertyName("data"),
        ]
        public WebhookData Data { get; set; }

        #region Helper Methods

        public static string WebhookTypeToString(List<WebhookType> types)
        {
            return string.Join(",", types.Select(x => WebhookTypeToString(x)));
        }

        public static string WebhookTypeToString(WebhookType type)
        {
            return type switch
            {
                WebhookType.Pokemon => "pokemon",
                WebhookType.Pokestops => "pokestops",
                WebhookType.Raids => "raids",
                WebhookType.Eggs => "eggs",
                WebhookType.Quests => "quests",
                WebhookType.Lures => "lures",
                WebhookType.Invasions => "invasions",
                WebhookType.Gyms => "gyms",
                WebhookType.Weather => "weather",
                _ => "pokemon",
            };
        }

        public static List<WebhookType> StringToWebhookTypes(string webhookTypes)
        {
            var list = new List<WebhookType>();
            var split = webhookTypes.Split(',');
            foreach (var item in split)
            {
                if (item.Contains("pokemon", System.StringComparison.OrdinalIgnoreCase))
                    list.Add(WebhookType.Pokemon);
                if (item.Contains("pokestops", System.StringComparison.OrdinalIgnoreCase))
                    list.Add(WebhookType.Pokestops);
                if (item.Contains("raids", System.StringComparison.OrdinalIgnoreCase))
                    list.Add(WebhookType.Raids);
                if (item.Contains("eggs", System.StringComparison.OrdinalIgnoreCase))
                    list.Add(WebhookType.Eggs);
                if (item.Contains("quests", System.StringComparison.OrdinalIgnoreCase))
                    list.Add(WebhookType.Quests);
                if (item.Contains("lures", System.StringComparison.OrdinalIgnoreCase))
                    list.Add(WebhookType.Lures);
                if (item.Contains("invasions", System.StringComparison.OrdinalIgnoreCase))
                    list.Add(WebhookType.Invasions);
                if (item.Contains("gyms", System.StringComparison.OrdinalIgnoreCase))
                    list.Add(WebhookType.Gyms);
                if (item.Contains("weather", System.StringComparison.OrdinalIgnoreCase))
                    list.Add(WebhookType.Weather);
            }
            return list;
        }

        #endregion
    }

    // TODO: Deserialize by name
    public enum WebhookType
    {
        Pokemon = 0,
        Pokestops,
        Raids,
        Eggs,
        Quests,
        Lures,
        Invasions,
        Gyms,
        Weather,
    }
}