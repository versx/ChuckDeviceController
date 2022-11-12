namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Common.Data.Contracts;

    [Table("api_key")]
    public class ApiKey : BaseEntity, IApiKey
    {
        [
            DatabaseGenerated(DatabaseGeneratedOption.Identity),
            JsonPropertyName("id"),
            DisplayName("ID"),
            Column("id"),
            Key,
        ]
        public uint Id { get; set; }

        [
            JsonPropertyName("name"),
            DisplayName("Name"),
            Column("name"),
            Required,
        ]
        public string Name { get; set; }

        [
            JsonPropertyName("key"),
            DisplayName("Key"),
            Column("key"),
            Required,
        ]
        public string? Key { get; set; }

        [
            JsonPropertyName("scope"),
            DisplayName("Scope"),
            Column("scope"),
            Required,
        ]
        public List<PluginApiKeyScope>? Scope { get; set; }

        [
            JsonPropertyName("expiration_timestamp"),
            DisplayName("Expires"),
            Column("expiration_timestamp"),
        ]
        public ulong ExpirationTimestamp { get; set; }

        [
            JsonPropertyName("enabled"),
            DisplayName("Enabled"),
            Column("enabled"),
            Required,
        ]
        public bool IsEnabled { get; set; }
    }
}