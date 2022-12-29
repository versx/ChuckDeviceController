namespace ChuckDeviceController.Plugin;

/// <summary>
/// <see cref="ISettingsProperty"/> class implementation used by plugins
/// to create UI setting elements in the dashboard.
/// </summary>
public class SettingsProperty : ISettingsProperty
{
    private const string DefaultClassName = "form-control";

    #region Properties

    /// <summary>
    /// Gets or sets the displayed text for the property, possibly
    /// used in a label.
    /// </summary>
    public string Text { get; set; } = null!;

    /// <summary>
    /// Gets or sets the ID and name of the element.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the initial value to set for the element.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets the default value to use for the element, if
    /// it supports it.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the type of HTML element to create.
    /// </summary>
    public SettingsPropertyType Type { get; set; }

    /// <summary>
    /// Gets or sets a value used for sorting each HTML element
    /// created for the properties.
    /// </summary>
    public uint DisplayIndex { get; set; }

    /// <summary>
    /// Gets or sets a value determining whether the HTML element
    /// value is required.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets a value determining whether to validate the
    /// value of the HTML element.
    /// </summary>
    public bool Validate { get; set; }

    /// <summary>
    /// Gets or sets the CSS class name to use.
    /// </summary>
    public string? Class { get; set; } = DefaultClassName;

    /// <summary>
    /// Gets or sets the raw CSS styling to use.
    /// </summary>
    public string? Style { get; set; }

    /// <summary>
    /// Gets or sets the group the settings property
    /// will be in.
    /// </summary>
    public SettingsPropertyGroup? Group { get; set; }

    #endregion

    #region Constructors

    /// <summary>
    /// Instantiates a new instance of the <see cref="SettingsProperty"/> class.
    /// </summary>
    public SettingsProperty()
    {
    }

    /// <summary>
    /// Instantiates a new instance of the <see cref="SettingsProperty"/> class.
    /// </summary>
    /// <param name="text">Text displayed for the property, possibly used in a label.</param>
    /// <param name="name">The ID and name of the element.</param>
    /// <param name="type">The type of HTML element to create.</param>
    /// <param name="value">Sets the initial value of the HTML element.</param>
    /// <param name="defaultValue">Default value of the HTML element, if it supports it.</param>
    /// <param name="displayIndex">Defines the sorting of the HTML element created for the properties.</param>
    /// <param name="isRequired">Determining whether or not the HTML element value is required.</param>
    /// <param name="validate">Determines whether or not to validate the value of the HTML element.</param>
    /// <param name="className">CSS class name to use.</param>
    /// <param name="style">Raw CSS styling to use.</param>
    /// <param name="group">Element group the settings property will be placed in.</param>
    public SettingsProperty(
        string text,
        string name,
        SettingsPropertyType type = SettingsPropertyType.Text,
        object? value = default,
        object? defaultValue = default,
        uint displayIndex = 999,
        bool isRequired = false,
        bool validate = false,
        string? className = null,
        string? style = null,
        SettingsPropertyGroup? group = null)
        : this()
    {
        Text = text;
        Name = name;
        Type = type;
        Value = value;
        DefaultValue = defaultValue;
        DisplayIndex = displayIndex;
        IsRequired = isRequired;
        Validate = validate;
        Class = className ?? (
            Type == SettingsPropertyType.CheckBox
                ? ""
                : DefaultClassName
        );
        Style = style;
        Group = group;
    }

    #endregion
}