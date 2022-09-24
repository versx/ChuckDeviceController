namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;

    using ChuckDeviceController.Common.Data.Contracts;

    [Table("api_key")]
    public class ApiKey : BaseEntity, IApiKey
    {
        [
            JsonPropertyName("id"),
            DisplayName("ID"),
            Column("id"),
            Key,
        ]
        public uint Id { get; set; }

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
    }
}