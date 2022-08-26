namespace ChuckDeviceController.Plugin
{
    /// <summary>
    /// Dashboard statistics item for displaying information
    /// on the front page.
    /// </summary>
    public class DashboardStatsItem : IDashboardStatsItem
    {
        /// <summary>
        /// Gets or sets the name or title of the statistic.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the value of the statistic.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets or sets a value determining whether the name
        /// and value properties include raw HTML or not.
        /// </summary>
        public bool IsHtml { get; }

        /// <summary>
        /// Instantiates a new dashboard statistics item using
        /// the provided property values.
        /// </summary>
        /// <param name="name">Name of the statistic.</param>
        /// <param name="value">Value of the statistic.</param>
        /// <param name="isHtml">Whether the name or value contain raw HTML.</param>
        public DashboardStatsItem(string name, string value, bool isHtml = false)
        {
            Name = name;
            Value = value;
            IsHtml = isHtml;
        }
    }
}