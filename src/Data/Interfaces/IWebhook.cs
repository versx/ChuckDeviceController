namespace ChuckDeviceController.Data.Interfaces
{
    public interface IWebhook
    {
        dynamic GetWebhookValues(string type);
    }
}