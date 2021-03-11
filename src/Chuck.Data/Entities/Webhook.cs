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
                if (item.ToLower().Contains("pokemon"))
                    list.Add(WebhookType.Pokemon);
                if (item.ToLower().Contains("pokestops"))
                    list.Add(WebhookType.Pokestops);
                if (item.ToLower().Contains("raids"))
                    list.Add(WebhookType.Raids);
                if (item.ToLower().Contains("eggs"))
                    list.Add(WebhookType.Eggs);
                if (item.ToLower().Contains("quests"))
                    list.Add(WebhookType.Quests);
                if (item.ToLower().Contains("lures"))
                    list.Add(WebhookType.Lures);
                if (item.ToLower().Contains("invasions"))
                    list.Add(WebhookType.Invasions);
                if (item.ToLower().Contains("gyms"))
                    list.Add(WebhookType.Gyms);
                if (item.ToLower().Contains("weather"))
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