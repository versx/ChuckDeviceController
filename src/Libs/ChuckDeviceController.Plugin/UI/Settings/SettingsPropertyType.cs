namespace ChuckDeviceController.Plugin
{
    /// <summary>
    /// Determines the type of HTML element to create for
    /// the settings property.
    /// </summary>
    public enum SettingsPropertyType
    {
        /// <summary>
        /// Settings property type is a text field.
        /// </summary>
        Text,

        /// <summary>
        /// Settings property type is a text area.
        /// </summary>
        TextArea,

        /// <summary>
        /// Settings property type is a numeric selector.
        /// </summary>
        Number,

        /// <summary>
        /// Settings property type is a checkbox field.
        /// </summary>
        CheckBox,

        /// <summary>
        /// Settings property type is a select item list element.
        /// </summary>
        Select,
    }
}