namespace WebhookProcessor
{
    using Chuck.Data.Entities;
    using WebhookProcessor.Queues;

    public class WebhookQueue<T> : BaseQueue<T>// where T : BaseEntity
    {
    }
}