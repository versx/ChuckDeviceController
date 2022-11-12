namespace ChuckDeviceController.Plugin
{
    /// <summary>
    /// 
    /// </summary>
    [AttributeUsage(validOn: AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class PluginApiKeyAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="apiKey"></param>
        public PluginApiKeyAttribute(string apiKey)
        {
            ApiKey = apiKey;
        }
    }
}