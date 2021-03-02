namespace Chuck.Infrastructure.Data.Interfaces
{
    public interface IWebhook
    {
        dynamic GetWebhookValues(string type);
    }
}