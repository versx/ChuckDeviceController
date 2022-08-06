namespace ChuckDeviceController.Data.Entities
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;
    using System.Text.Json.Serialization;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Geometry.Models;

    [Table("webhook")]
    public class Webhook : BaseEntity, IWebhook
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
            DisplayName("Types"),
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

        [
            NotMapped,
            JsonPropertyName("multiPolygons"),
        ]
        public List<List<Coordinate>> GeofenceMultiPolygons { get; set; }

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
                WebhookType.AlternativeQuests => "alternative_quests",
                WebhookType.Lures => "lures",
                WebhookType.Invasions => "invasions",
                WebhookType.Gyms => "gyms",
                WebhookType.GymInfo => "gym_info",
                WebhookType.Weather => "weather",
                WebhookType.Accounts => "accounts",
                _ => "pokemon",
            };
        }

        public static List<WebhookType> StringToWebhookTypes(string webhookTypes)
        {
            var list = new List<WebhookType>();
            var split = webhookTypes.Split(',');
            foreach (var item in split)
            {
                switch (item.ToLower())
                {
                    case "pokemon":
                        list.Add(WebhookType.Pokemon);
                        break;
                    case "pokestops":
                        list.Add(WebhookType.Pokestops);
                        break;
                    case "raids":
                        list.Add(WebhookType.Raids);
                        break;
                    case "eggs":
                        list.Add(WebhookType.Eggs);
                        break;
                    case "quests":
                        list.Add(WebhookType.Quests);
                        break;
                    case "alternative_quests":
                        list.Add(WebhookType.AlternativeQuests);
                        break;  
                    case "lures":
                        list.Add(WebhookType.Lures);
                        break;
                    case "invasions":
                        list.Add(WebhookType.Invasions);
                        break;
                    case "gyms":
                        list.Add(WebhookType.Gyms);
                        break;
                    case "gym_info":
                        list.Add(WebhookType.GymInfo);
                        break;
                    case "weather":
                        list.Add(WebhookType.Weather);
                        break;
                    case "accounts":
                        list.Add(WebhookType.Accounts);
                        break;
                }
            }
            return list;
        }

        #endregion
    }
}