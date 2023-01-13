namespace ChuckDeviceConfigurator.Services.Webhooks;

using ChuckDeviceController.Data.Entities;

/// <summary>
/// Caches all configured IV lists to reduce database loads.
/// </summary>
public interface IWebhookControllerService : IControllerService<Webhook, string>
{
    Task<IEnumerable<Webhook>> GetAllAsync(bool includeGeofenceMultiPolygons = false);
}