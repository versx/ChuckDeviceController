namespace ChuckDeviceController.Data.Abstractions;

using ChuckDeviceController.Data.Common;

public interface IWebhook : IBaseEntity
{
    string Name { get; }

    WebhookType Types { get; }

    double Delay { get; }

    string Url { get; }

    bool Enabled { get; }

    List<string> Geofences { get; }

    WebhookData? Data { get; }
}