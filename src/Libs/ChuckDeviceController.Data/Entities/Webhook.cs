namespace ChuckDeviceController.Data.Entities;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

using ChuckDeviceController.Common;
using ChuckDeviceController.Common.Abstractions;
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
    public string Name { get; set; } = null!;

    [
        DisplayName("Types"),
        Column("types"),
        JsonPropertyName("types"),
    ]
    public WebhookType Types { get; set; }

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
    public string Url { get; set; } = null!;

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
    public List<string> Geofences { get; set; } = new();

    [
        DisplayName("Data"),
        Column("data"),
        JsonPropertyName("data"),
        //JsonConverter(typeof(ObjectDataConverter<WebhookData>)),
    ]
    public WebhookData? Data { get; set; }

    [
        NotMapped,
        JsonPropertyName("multiPolygons"),
    ]
    public List<List<Coordinate>>? GeofenceMultiPolygons { get; set; }

    #endregion

}