namespace Chuck.Data.Interfaces
{
    public interface IWebhook
    {
        dynamic GetWebhookValues(string type);
    }
}