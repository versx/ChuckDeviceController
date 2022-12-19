namespace ChuckDeviceController.JobControllers.Utilities;

public static class Utils
{
    public static string GetQueueLink(
        string instanceName,
        string displayText = "Queue",
        string basePath = "/Instance/IvQueue",
        bool html = false)
    {
        var encodedName = Uri.EscapeDataString(instanceName);
        var url = $"{basePath}/{encodedName}";
        var status = $"<a href='{url}'>{displayText}</a>";
        return html
            ? status
            : url;
    }
}