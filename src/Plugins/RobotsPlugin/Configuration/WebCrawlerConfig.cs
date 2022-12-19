namespace RobotsPlugin.Configuration;

public class WebCrawlerConfig
{
    public bool UseHoneyPotService { get; set; }

    public string? HoneyPotRoute { get; set; }

    public bool ProcessStaticFiles { get; set; }

    public List<string> StaticFileExtensions { get; set; } = new();
}