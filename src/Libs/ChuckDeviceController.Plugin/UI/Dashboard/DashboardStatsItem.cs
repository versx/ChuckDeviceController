namespace ChuckDeviceController.Plugin;

using System.Text.Json.Serialization;

/// <summary>
/// <see cref="IDashboardStatsItem"/> class implementation
/// for displaying information on the front page.
/// </summary>
public class DashboardStatsItem : IDashboardStatsItem
{
    /// <summary>
    /// Gets the name or title of the statistic.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the value of the statistic.
    /// </summary>
    public string Value => ValueUpdater != null ? ValueUpdater() : string.Empty;

    /// <summary>
    /// Gets a value determining whether the name
    /// and value properties include raw HTML or not.
    /// </summary>
    public bool IsHtml { get; }

    /// <summary>
    /// Gets the function to update the value for the
    /// dashboard statistic item.
    /// </summary>
    [JsonIgnore]
    public Func<string> ValueUpdater { get; }

    /// <summary>
    /// Instantiates a new instance of the <see cref="DashboardStatsItem"/> class.
    /// </summary>
    /// <param name="name">Name of the statistic.</param>
    /// <param name="isHtml">Whether or not the name or value contains raw HTML.</param>
    /// <param name="valueUpdater">Function to update the value for the dashboard statistic item.</param>
    public DashboardStatsItem(string name, bool isHtml = false, Func<string> valueUpdater = default!)
    {
        Name = name;
        IsHtml = isHtml;
        ValueUpdater = valueUpdater;
    }
}