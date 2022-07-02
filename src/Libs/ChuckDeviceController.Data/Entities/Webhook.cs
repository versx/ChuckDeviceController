namespace ChuckDeviceController.Data.Entities
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;
    using System.Text.Json.Serialization;

    [Table("webhook")]
    public class Webhook : BaseEntity
    {
        #region Properties

        [
            DisplayName("Name"),
            Column("name"),
            Key,
            JsonPropertyName("name"),
        ]
        public string Name { get; set; }

        [
            DisplayName("Webhook Types"),
            Column("types"),
            JsonPropertyName("types"),
        ]
        public List<WebhookType> Types { get; set; }

        [
            DisplayName("Delay"),
            Column("delay"),
            JsonPropertyName("delay"),
        ]
        public double Delay { get; set; }

        [
            DisplayName("Url"),
            Column("url"),
            JsonPropertyName("url"),
        ]
        public string Url { get; set; }

        [
            DisplayName("Enabled"),
            Column("enabled"),
            JsonPropertyName("enabled"),
        ]
        public bool Enabled { get; set; }

        [
            DisplayName("Geofences"),
            Column("geofences"),
            JsonPropertyName("geofences"),
        ]
        public List<string> Geofences { get; set; }

        [
            DisplayName("Data"),
            Column("data"),
            JsonPropertyName("data"),
        ]
        public WebhookData Data { get; set; }

        #endregion

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
}