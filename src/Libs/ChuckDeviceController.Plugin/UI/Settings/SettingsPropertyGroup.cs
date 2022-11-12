namespace ChuckDeviceController.Plugin
{
    /// <summary>
    /// Interface contract for grouping settings properties.
    /// </summary>
    public class SettingsPropertyGroup : ISettingsPropertyGroup, IEquatable<SettingsPropertyGroup>
    {
        /// <summary>
        /// Gets or sets the unique ID for the settings property group.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the display text for the settings property group.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets a value used for sorting each HTML element
        /// created for the properties.
        /// </summary>
        public uint DisplayIndex { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public SettingsPropertyGroup()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="text"></param>
        /// <param name="displayIndex"></param>
        public SettingsPropertyGroup(string id, string text, uint displayIndex)
        {
            Id = id;
            Text = text;
            DisplayIndex = displayIndex;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return (int)((Id?.GetHashCode() ?? 0) ^ (Text?.GetHashCode() ?? 0) ^ DisplayIndex);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object? obj)
        {
            var other = obj as SettingsPropertyGroup;
            return Equals(other);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(SettingsPropertyGroup? other)
        {
            var result = other != null && Text == other.Text;
            return result;
        }
    }
}