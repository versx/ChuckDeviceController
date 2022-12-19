namespace ChuckDeviceController.Plugin;

/// <summary>
/// Settings property interface contract used by plugins to
/// create UI setting elements in the dashboard.
/// </summary>
public class SettingsProperty : ISettingsProperty
{
    private const string DefaultClassName = "form-control";

    #region Properties

    /// <summary>
    /// Gets or sets the displayed text for the property, possibly
    /// used in a label.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets the ID and name of the element.
    /// </summary>
    public string Name { get; set; }

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
    public string? Class { get; set; }

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
    /// 
    /// </summary>
    public SettingsProperty()
    {
        Class = DefaultClassName;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="text"></param>
    /// <param name="name"></param>
    /// <param name="type"></param>
    /// <param name="value"></param>
    /// <param name="defaultValue"></param>
    /// <param name="displayIndex"></param>
    /// <param name="isRequired"></param>
    /// <param name="validate"></param>
    /// <param name="className"></param>
    /// <param name="style"></param>
    /// <param name="group"></param>
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