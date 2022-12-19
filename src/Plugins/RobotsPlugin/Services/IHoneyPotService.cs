namespace RobotsPlugin.Services;

public interface IHoneyPotService
{
    void OnTriggered(string ipAddress, string userAgent);
}
