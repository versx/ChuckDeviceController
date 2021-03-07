namespace DataConsumer.Models
{
    using System.Text.Json.Serialization;

    public class QuestFound
    {
        [JsonPropertyName("raw")]
        public string Raw { get; set; }
    }
}