namespace ChuckDeviceController.Data.Contracts
{
    public interface IWebhookPayload
    {
        dynamic GetWebhookData(string type);
    }
}