namespace WebhookProcessor
{
    using Chuck.Infrastructure.Data.Entities;
    using Chuck.Infrastructure.Queues;

    public class WebhookQueue<T> : BaseQueue<T>// where T : BaseEntity
    {
    }
}