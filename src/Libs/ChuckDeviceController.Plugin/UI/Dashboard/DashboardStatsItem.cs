namespace ChuckDeviceController.Plugin;

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
    public string Value { get; }

    /// <summary>
    /// Gets a value determining whether the name
    /// and value properties include raw HTML or not.
    /// </summary>
    public bool IsHtml { get; }

    /// <summary>
    /// Instantiates a new instance of the <see cref="DashboardStatsItem"/> class.
    /// </summary>
    /// <param name="name">Name of the statistic.</param>
    /// <param name="value">Value of the statistic.</param>
    /// <param name="isHtml">Whether or not the name or value contains raw HTML.</param>
    public DashboardStatsItem(string name, string value, bool isHtml = false)
    {
        Name = name;
        Value = value;
        IsHtml = isHtml;
    }
}