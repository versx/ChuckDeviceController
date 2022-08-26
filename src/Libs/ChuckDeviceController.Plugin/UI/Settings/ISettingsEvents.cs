namespace ChuckDeviceController.Plugin
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISettingsPropertyEvents
    {
        /// <summary>
        /// 
        /// </summary>
        void OnClick(ISettingsProperty property);

        /// <summary>
        /// 
        /// </summary>
        void OnToggle(ISettingsProperty property);

        /// <summary>
        /// 
        /// </summary>
        void OnSave(ISettingsProperty property);
    }
}