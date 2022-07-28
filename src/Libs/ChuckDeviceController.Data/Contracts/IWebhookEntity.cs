namespace ChuckDeviceController.Data.Contracts
{
    public interface IWebhookEntity
    {
        dynamic GetWebhookData(string type);
    }
}