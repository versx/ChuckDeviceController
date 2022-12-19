namespace ChuckDeviceCommunicator.Services;

using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Protos;

public interface IWebhookRelayService
{
    /// <summary>
    /// Gets a value determining whether the webhook relay
    /// service is running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Gets a value incidating how many total webhooks
    /// have been sent during this session.
    /// </summary>
    ulong TotalSent { get; }

    /// <summary>
    /// Gets a list of available webhook endpoints to send data to.
    /// </summary>
    IEnumerable<Webhook> WebhookEndpoints { get; }


    /// <summary>
    /// Starts the webhook relay service.
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// Stops the webhook relay service.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Reloads the webhook relay service.
    /// </summary>
    Task ReloadAsync();

    /// <summary>
    /// Enqueues a received webhook JSON payload to be processed and sent.
    /// </summary>
    /// <param name="webhookType">Webhook payload type to deserialize JSON payload as.</param>
    /// <param name="json">JSON payload of webhook entity.</param>
    Task EnqueueAsync(WebhookPayloadType webhookType, string json);
}