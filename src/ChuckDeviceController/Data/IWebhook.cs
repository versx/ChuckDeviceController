namespace ChuckDeviceController.Data
{
    public interface IWebhookPayload
    {
        dynamic GetWebhookValues(string type);
    }
}