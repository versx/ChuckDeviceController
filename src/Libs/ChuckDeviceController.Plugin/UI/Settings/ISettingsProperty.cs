namespace ChuckDeviceController.Plugin
{
    /// <summary>
    /// Settings property interface contract used by plugins to
    /// create UI setting elements in the dashboard.
    /// </summary>
    public interface ISettingsProperty
    {
        /// <summary>
        /// Gets or sets the displayed text for the property, possibly
        /// used in a label.
        /// </summary>
        string Text { get; }

        /// <summary>
        /// Gets or sets the ID and name of the element.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets or sets the initial value to set for the element.
        /// </summary>
        object? Value { get; }

        /// <summary>
        /// Gets or sets the default value to use for the element, if
        /// it supports it.
        /// </summary>
        object? DefaultValue { get; }

        /// <summary>
        /// Gets or sets the type of HTML element to create.
        /// </summary>
        SettingsPropertyType Type { get; }

        /// <summary>
        /// Gets or sets a value used for sorting each HTML element
        /// created for the properties.
        /// </summary>
        uint DisplayIndex { get; }

        /// <summary>
        /// Gets or sets a value determining whether the HTML element
        /// value is required.
        /// </summary>
        bool IsRequired { get; }

        /// <summary>
        /// Gets or sets a value determining whether to validate the
        /// value of the HTML element.
        /// </summary>
        bool Validate { get; }

        /// <summary>
        /// Gets or sets the CSS class name to use.
        /// </summary>
        string? Class { get; }

        /// <summary>
        /// Gets or sets the raw CSS styling to use.
        /// </summary>
        string? Style { get; }
    }
}