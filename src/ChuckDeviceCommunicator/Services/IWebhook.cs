namespace ChuckDeviceController.Net
{
    public interface IWebhookPayload
    {
        dynamic GetWebhookValues(string type);
    }
}