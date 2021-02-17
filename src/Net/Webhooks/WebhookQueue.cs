namespace ChuckDeviceController.Net.Webhooks
{
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Services.Queues;

    public class WebhookQueue<T> : BaseQueue<T> where T : BaseEntity
    {
    }
}