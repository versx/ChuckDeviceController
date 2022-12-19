namespace ChuckDeviceController.Data.Abstractions;

public interface IWebhookEntity
{
    dynamic? GetWebhookData(string type);
}