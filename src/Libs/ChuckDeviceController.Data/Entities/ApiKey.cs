namespace ChuckDeviceController.Data.Entities;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

using ChuckDeviceController.Common;
using ChuckDeviceController.Common.Abstractions;

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
    public string Name { get; set; } = null!;

    [
        JsonPropertyName("key"),
        DisplayName("Key"),
        Column("key"),
        Required,
    ]
    public string Key { get; set; } = null!;

    [
        JsonPropertyName("scope"),
        DisplayName("Scope"),
        Column("scope"),
    ]
    public PluginApiKeyScope Scope { get; set; } = PluginApiKeyScope.None;

    [
        JsonPropertyName("expiration_timestamp"),
        DisplayName("Expires"),
        Column("expiration_timestamp"),
    ]
    public ulong ExpirationTimestamp { get; set; } = 0;

    [
        JsonPropertyName("enabled"),
        DisplayName("Enabled"),
        Column("enabled"),
        Required,
    ]
    public bool IsEnabled { get; set; }
}