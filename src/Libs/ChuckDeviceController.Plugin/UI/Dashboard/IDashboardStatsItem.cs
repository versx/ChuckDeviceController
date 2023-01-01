namespace ChuckDeviceController.Plugin;

using System.Text.Json.Serialization;

/// <summary>
/// Dashboard statistics item interface contract for
/// displaying information on the front page.
/// </summary>
public interface IDashboardStatsItem
{
    /// <summary>
    /// Gets or sets the name or title of the statistic.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets or sets the value of the statistic.
    /// </summary>
    string Value { get; }

    /// <summary>
    /// Gets or sets a value determining whether the name
    /// and value properties include raw HTML or not.
    /// </summary>
    bool IsHtml { get; }

    /// <summary>
    /// Gets the function to update the value for the
    /// dashboard tile.
    /// </summary>
    [JsonIgnore]
    Func<string> ValueUpdater { get; }
}