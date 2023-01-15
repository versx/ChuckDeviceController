namespace ChuckDeviceController.Common.Abstractions;

public interface IWebhookEntity
{
    dynamic? GetWebhookData(string type);
}